using KpiLib.Dims;
using KpiLib.Models;

namespace KpiLib;

// ======================================
//  In-memory "DB" and KPI compute engine
// ======================================
public partial class KpiDb
{
    // Auto-increment counters (simulate IDENTITY)
    private long _idValue = 0, _idUnit = 0, _idContext = 0, _idFormula = 0, _idTs = 0, _idSrc = 0, _idMap = 0, _idStg = 0;

    public readonly List<ValueDim> Values = new();
    public readonly List<UnitDim> Units = new();
    public readonly List<ContextDim> Contexts = new();
    public readonly List<FormulaDim> Formulas = new();
    public readonly List<FormulaInput> FormulaInputs = new();
    public readonly List<ValueTimeSeries> Facts = new();
    public readonly List<UnitConversion> Conversions = new();
    public readonly List<ExternalSourceDim> Sources = new();
    public readonly List<ExternalValueMap> Maps = new();
    public readonly List<ParamFeedStg> Staging = new();

    public long NextValueId() => ++_idValue;
    public long NextUnitId() => ++_idUnit;
    public long NextContextId() => ++_idContext;
    public long NextFormulaId() => ++_idFormula;
    public long NextTsId() => ++_idTs;
    public long NextSrcId() => ++_idSrc;
    public long NextMapId() => ++_idMap;
    public long NextStgId() => ++_idStg;

    // Helpers "dimension lookup at time"
    public ValueDim? FindValueAt(string code, DateTime t) =>
        Values.Where(v => v.ValueCode == code && v.ValidFrom <= t && (v.ValidTo == null || t < v.ValidTo))
              .OrderByDescending(v => v.ValidFrom).FirstOrDefault();

    public UnitDim? FindUnitAt(string code, DateTime t) =>
        Units.Where(u => u.UnitCode == code && u.ValidFrom <= t && (u.ValidTo == null || t < u.ValidTo))
             .OrderByDescending(u => u.ValidFrom).FirstOrDefault();

    public ContextDim? FindContextAt(string code, DateTime t) =>
        Contexts.Where(c => c.ContextCode == code && c.ValidFrom <= t && (c.ValidTo == null || t < c.ValidTo))
                .OrderByDescending(c => c.ValidFrom).FirstOrDefault();

    public FormulaDim? FindFormulaForValueAt(long valueId, DateTime t) =>
        Formulas.Where(f => f.ValueId == valueId && f.ValidFrom <= t && (f.ValidTo == null || t < f.ValidTo))
                .OrderByDescending(f => f.ValidFrom).FirstOrDefault();

    public IEnumerable<FormulaInput> InputsOf(long formulaId) =>
        FormulaInputs.Where(fi => fi.FormulaId == formulaId).OrderBy(fi => fi.InputAlias);

    // Add-or-keep helpers (idempotent by key + valid_from)
    public UnitDim EnsureUnit(string code, string name, DateTime validFrom)
    {
        var found = Units.FirstOrDefault(u => u.UnitCode == code && u.ValidFrom == validFrom);
        if (found != null) return found;
        var u = new UnitDim { Id = NextUnitId(), UnitCode = code, UnitName = name, ValidFrom = validFrom };
        Units.Add(u);
        return u;
    }

    public ContextDim EnsureContext(string code, string? name, string? plant, DateTime validFrom)
    {
        var found = Contexts.FirstOrDefault(c => c.ContextCode == code && c.ValidFrom == validFrom);
        if (found != null) return found;
        var c = new ContextDim { Id = NextContextId(), ContextCode = code, ContextName = name, Plant = plant, ValidFrom = validFrom };
        Contexts.Add(c);
        return c;
    }

    public ValueDim EnsureValue(string code, string name, string kind, string unitCode, string? category, DateTime validFrom)
    {
        var found = Values.FirstOrDefault(v => v.ValueCode == code && v.ValidFrom == validFrom);
        if (found != null) return found;

        var unit = EnsureUnit(unitCode, unitCode, validFrom); // name already set earlier by unit seed
        var v = new ValueDim
        {
            Id = NextValueId(),
            ValueCode = code,
            ValueName = name,
            ValueKind = kind,
            DefaultUnitId = unit.Id,
            Category = category,
            ValidFrom = validFrom
        };
        Values.Add(v);
        return v;
    }

    public void EnsureConversion(string fromUnit, string toUnit, DateTime t, double factor, double offset)
    {
        var uFrom = FindUnitAt(fromUnit, t) ?? throw new Exception($"Unit {fromUnit} @ {t} missing");
        var uTo = FindUnitAt(toUnit, t) ?? throw new Exception($"Unit {toUnit} @ {t} missing");
        if (!Conversions.Any(c => c.UnitFrom == uFrom.Id && c.UnitTo == uTo.Id))
            Conversions.Add(new UnitConversion { UnitFrom = uFrom.Id, UnitTo = uTo.Id, Factor = factor, Offset = offset });
    }

    public FormulaDim EnsureFormula(string valueCode, DateTime t, string expr, string? notes, Dictionary<string, string> aliasToValueCode)
    {
        var v = FindValueAt(valueCode, t) ?? throw new Exception($"value_dim {valueCode} @ {t} missing");
        var exists = FindFormulaForValueAt(v.Id, t);
        if (exists != null && exists.ValidFrom == t) return exists;

        var f = new FormulaDim
        {
            Id = NextFormulaId(),
            ValueId = v.Id,
            Expression = expr,
            Notes = notes,
            ValidFrom = t
        };
        Formulas.Add(f);

        // Anti-cycle check: will throw if cycle is created
        foreach (var kv in aliasToValueCode.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var inputV = FindValueAt(kv.Value, t) ?? throw new Exception($"value_dim {kv.Value} @ {t} missing");
            var fi = new FormulaInput { FormulaId = f.Id, InputValueId = inputV.Id, InputAlias = kv.Key };
            // tentative add
            FormulaInputs.Add(fi);
            if (HasCycle())
            {
                FormulaInputs.Remove(fi);
                throw new Exception("Dipendenza circolare rilevata nelle formule");
            }
        }
        return f;
    }

    // Graph: edge src -> dst (dst depends on src)
    private bool HasCycle()
    {
        var dstByFormula = Formulas.ToDictionary(x => x.Id, x => x.ValueId);
        var edges = FormulaInputs.Select(e => (Src: e.InputValueId, Dst: dstByFormula[e.FormulaId])).ToList();

        var adj = new Dictionary<long, List<long>>();
        foreach (var e in edges)
        {
            if (!adj.ContainsKey(e.Dst)) adj[e.Dst] = new List<long>();
            adj[e.Dst].Add(e.Src);
        }

        var visited = new HashSet<long>();
        var stack = new HashSet<long>();

        bool Dfs(long node)
        {
            if (stack.Contains(node)) return true;
            if (visited.Contains(node)) return false;
            visited.Add(node);
            stack.Add(node);
            if (adj.TryGetValue(node, out var list))
            {
                foreach (var nxt in list)
                    if (Dfs(nxt)) return true;
            }
            stack.Remove(node);
            return false;
        }

        foreach (var n in adj.Keys)
            if (Dfs(n)) return true;

        return false;
    }
}
