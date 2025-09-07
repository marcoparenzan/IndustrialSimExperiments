using KpiLib.Models;

namespace KpiLib;

public partial class KpiDb
{
    public double Epsilon = 1e-9;

    public long UpsertIfChangedByCodes(
        string valueCode, string contextCode, string unitCode, double value,
        string quality = "ok", string? calcMethod = null, string? sourceRunId = null,
        DateTime? now = null)
    {
        var t = now ?? DateTime.UtcNow;
        var v = FindValueAt(valueCode, t) ?? throw new Exception("value_dim non trovato");
        var c = FindContextAt(contextCode, t) ?? throw new Exception("context_dim non trovato");
        var u = FindUnitAt(unitCode, t) ?? throw new Exception("unit_dim non trovato");

        var current = Facts.Where(f => f.ValueId == v.Id && f.ContextId == c.Id && f.ValidTo == null)
                           .OrderByDescending(f => f.ValidFrom)
                           .FirstOrDefault();

        if (current == null)
        {
            var rec = new ValueTimeSeries
            {
                Id = NextTsId(),
                ValueId = v.Id,
                ContextId = c.Id,
                UnitId = u.Id,
                DoubleValue = value,
                ValidFrom = t,
                QualityFlag = quality,
                CalcMethod = calcMethod,
                SourceRunId = sourceRunId
            };
            Facts.Add(rec);
            return rec.Id;
        }

        if (current.UnitId == u.Id &&
            Math.Abs(current.DoubleValue - value) <= Epsilon &&
            string.Equals(current.QualityFlag ?? "", quality ?? "", StringComparison.OrdinalIgnoreCase))
        {
            return current.Id;
        }

        current.ValidTo = t;
        var rec2 = new ValueTimeSeries
        {
            Id = NextTsId(),
            PrevId = current.Id,
            ValueId = v.Id,
            ContextId = c.Id,
            UnitId = u.Id,
            DoubleValue = value,
            ValidFrom = t,
            QualityFlag = quality,
            CalcMethod = calcMethod,
            SourceRunId = sourceRunId
        };
        Facts.Add(rec2);
        return rec2.Id;
    }

    // Compute a value (only method-based engine retained)
    public double ComputeAtTime(string valueCode, string contextCode, DateTime t,
                                string? unitOut = null, bool doUpsert = false,
                                string quality = "ok", string? sourceRunId = null)
    {
        var v = FindValueAt(valueCode, t) ?? throw new Exception("value_dim/context non trovato");
        var formula = FindFormulaForValueAt(v.Id, t) ?? throw new Exception("Nessuna formula valida");
        if (formula.Engine != "method")
            throw new NotSupportedException("Only 'method' engine is supported (expression evaluator removed).");

        return InvokeMethodFormula(formula, t, contextCode, unitOut, doUpsert, quality, sourceRunId);
    }

    // Recursive compute stays the same; it now ultimately uses only method formulas.
    public double ComputeRecursive(string valueCode, string contextCode, DateTime t,
                                   string? unitOut = null, string quality = "ok",
                                   string? sourceRunId = null, HashSet<string>? visited = null)
    {
        visited ??= new HashSet<string>(StringComparer.Ordinal);
        if (visited.Contains(valueCode))
            throw new Exception("Ciclo di dipendenze – controlla le formule");
        visited.Add(valueCode);

        var v = FindValueAt(valueCode, t) ?? throw new Exception($"Value {valueCode} @ {t} non risolto");
        var c = FindContextAt(contextCode, t) ?? throw new Exception($"Context {contextCode} @ {t} non risolto");

        // existing fact?
        var existing = Facts.Where(f => f.ValueId == v.Id && f.ContextId == c.Id &&
                                        f.ValidFrom <= t && (f.ValidTo == null || t < f.ValidTo))
                            .OrderByDescending(f => f.ValidFrom)
                            .FirstOrDefault();
        if (existing != null)
            return existing.DoubleValue;

        var formula = FindFormulaForValueAt(v.Id, t);
        if (formula == null)
            throw new Exception($"Nessuna formula valida per {valueCode} e nessun valore presente (servono PARAM)");

        // Ensure dependencies
        foreach (var fi in InputsOf(formula.Id))
        {
            var inputCode = Values.First(x => x.Id == fi.InputValueId).ValueCode;
            var have = Facts.Any(f => f.ValueId == fi.InputValueId && f.ContextId == c.Id &&
                                      f.ValidFrom <= t && (f.ValidTo == null || t < f.ValidTo));
            if (!have)
                _ = ComputeRecursive(inputCode, contextCode, t, null, quality, sourceRunId, visited);
        }

        return ComputeAtTime(valueCode, contextCode, t, unitOut, doUpsert: true, quality: quality, sourceRunId: sourceRunId);
    }
}
