using System.Reflection;
using KpiLib.Dims;
using KpiLib.Models;

namespace KpiLib;

/// <summary>
/// Estensione del motore KPI: supporto formule = metodi statici (reflection).
/// Engine = "method", FormulaDim.Expression contiene FullName del metodo es: "KpiApp.OeeFormulaMethods.OEE".
/// Gli alias degli input DEVONO corrispondere esattamente ai nomi dei parametri del metodo.
/// </summary>
public partial class KpiDb
{
    /// <summary>
    /// Registra una formula che punta ad un metodo statico (senza creare duplicati alla stessa validFrom).
    /// fullMethodName: Namespace.Tipo.Metodo  (ultimo token = nome metodo, tutto il resto = nome tipo completo)
    /// </summary>
    public FormulaDim EnsureFormulaMethod(
        string valueCode,
        DateTime t,
        string fullMethodName,
        string? notes,
        Dictionary<string, string> aliasToValueCode)
    {
        var v = FindValueAt(valueCode, t) ?? throw new Exception($"value_dim {valueCode} @ {t} mancante");
        var exists = FindFormulaForValueAt(v.Id, t);
        if (exists != null && exists.ValidFrom == t && exists.Engine == "method" && exists.Expression == fullMethodName)
            return exists;

        var f = new FormulaDim
        {
            Id = NextFormulaId(),
            ValueId = v.Id,
            Engine = "method",
            Expression = fullMethodName,
            Notes = notes,
            ValidFrom = t
        };
        Formulas.Add(f);

        // Input + anti ciclo
        foreach (var kv in aliasToValueCode.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var inputV = FindValueAt(kv.Value, t) ?? throw new Exception($"value_dim {kv.Value} @ {t} mancante");
            var fi = new FormulaInput { FormulaId = f.Id, InputValueId = inputV.Id, InputAlias = kv.Key };
            FormulaInputs.Add(fi);
            if (HasCycle())
            {
                FormulaInputs.Remove(fi);
                throw new Exception("Dipendenza circolare rilevata (method engine)");
            }
        }

        // Validazione metodo
        ValidateMethodSignature(fullMethodName, f);

        return f;
    }

    private static (string typeName, string methodName) SplitFullMethodName(string full)
    {
        var lastDot = full.LastIndexOf('.');
        if (lastDot < 1 || lastDot == full.Length - 1)
            throw new Exception($"Nome metodo non valido: {full}");
        return (full[..lastDot], full[(lastDot + 1)..]);
    }

    private void ValidateMethodSignature(string fullMethodName, FormulaDim formula)
    {
        var (typeName, methodName) = SplitFullMethodName(fullMethodName);
        var type = AppDomain.CurrentDomain.GetAssemblies()
                          .Select(a => a.GetType(typeName, throwOnError: false, ignoreCase: false))
                          .FirstOrDefault(t => t != null)
                   ?? throw new Exception($"Tipo non trovato: {typeName}");
        var mi = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                 ?? throw new Exception($"Metodo {methodName} non trovato in {typeName}");

        // Controllo corrispondenza alias -> param names
        var paramNames = InputsOf(formula.Id).Select(i => i.InputAlias).ToHashSet(StringComparer.Ordinal);
        foreach (var p in mi.GetParameters())
        {
            if (!paramNames.Contains(p.Name!))
                throw new Exception($"Parametro '{p.Name}' del metodo {fullMethodName} non ha alias corrispondente");
            if (p.ParameterType != typeof(double))
                throw new Exception($"Parametro '{p.Name}' del metodo {fullMethodName} non è double (solo double supportato)");
        }
    }

    private double InvokeMethodFormula(FormulaDim formula, DateTime t, string contextCode,
                                       string? unitOut, bool doUpsert, string quality, string? sourceRunId)
    {
        var (typeName, methodName) = SplitFullMethodName(formula.Expression);

        // Ottieni context e input values
        var c = FindContextAt(contextCode, t) ?? throw new Exception("context non trovato");
        var inputs = InputsOf(formula.Id).ToList();
        if (inputs.Count == 0) throw new Exception("Formula metodo senza input");

        // Mappa alias -> (valore, unitId)
        var aliasVals = new Dictionary<string, (double val, long unitId)>(StringComparer.Ordinal);
        foreach (var fi in inputs)
        {
            var cur = Facts.Where(f => f.ValueId == fi.InputValueId && f.ContextId == c.Id &&
                                       f.ValidFrom <= t && (f.ValidTo == null || t < f.ValidTo))
                           .OrderByDescending(f => f.ValidFrom).FirstOrDefault()
                      ?? throw new Exception($"Valore input '{fi.InputAlias}' mancante");
            aliasVals[fi.InputAlias] = (cur.DoubleValue, cur.UnitId);
        }

        // NOTA: non forziamo la conversione delle unità degli input ad una sola unit
        // perché le formule codice implementano esplicitamente le conversioni (es *60 /60).
        // (unitOut ignorato per ora – estendibile alla conversione output)

        // Risolvi tipo/metodo
        var type = AppDomain.CurrentDomain.GetAssemblies()
                          .Select(a => a.GetType(typeName, throwOnError: false, ignoreCase: false))
                          .FirstOrDefault(t2 => t2 != null)
                   ?? throw new Exception($"Tipo non caricato: {typeName}");
        var mi = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                 ?? throw new Exception($"Metodo statico {methodName} non trovato in {typeName}");

        var parameters = mi.GetParameters();
        var args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (!aliasVals.TryGetValue(p.Name!, out var tpl))
                throw new Exception($"Alias '{p.Name}' non trovato per metodo {formula.Expression}");
            args[i] = tpl.val;
        }

        var raw = mi.Invoke(null, args);
        if (raw is not double result)
            throw new Exception("Metodo non restituisce double");

        if (doUpsert)
        {
            // Usa unità di default del primo input (comportamento semplice)
            var firstUnitCode = Units.First(u => u.Id == aliasVals.Values.First().unitId).UnitCode;
            UpsertIfChangedByCodes(
                valueCode: Values.First(v => v.Id == formula.ValueId).ValueCode,
                contextCode: contextCode,
                unitCode: firstUnitCode,
                value: result,
                quality: quality,
                calcMethod: "method",
                sourceRunId: sourceRunId,
                now: t);
        }

        return result;
    }
}