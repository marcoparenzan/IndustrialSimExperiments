using KpiLib.Dims;

namespace KpiLib;

public partial class KpiDb
{
    public double RecomputeAtTime(string valueCode, string contextCode, DateTime t,
                                  string? unitOut = null, string quality = "ok",
                                  string? sourceRunId = null)
    {
        var v = FindValueAt(valueCode, t) ?? throw new Exception($"Value {valueCode} @ {t} non trovato");
        var c = FindContextAt(contextCode, t) ?? throw new Exception($"Context {contextCode} @ {t} non trovato");

        var formula = FindFormulaForValueAt(v.Id, t); // può essere null per PARAM / leaf

        if (formula == null)
        {
            var existingLeaf = Facts.Where(f => f.ValueId == v.Id && f.ContextId == c.Id &&
                                                f.ValidFrom <= t && (f.ValidTo == null || t < f.ValidTo))
                                    .OrderByDescending(f => f.ValidFrom).FirstOrDefault()
                               ?? throw new Exception($"Valore PARAM '{valueCode}' mancante (nessuna formula e nessun fact).");
            return existingLeaf.DoubleValue;
        }

        // Calcolo formula (ComputeAtTime esegue invocazione metodo e upsert se cambia)
        return ComputeAtTime(valueCode, contextCode, t,
                             unitOut: unitOut,
                             doUpsert: true,
                             quality: quality,
                             sourceRunId: sourceRunId);
    }

    /// <summary>
    /// Ricalcolo ricorsivo forzato con:
    /// - Rimozione del falso positivo di ciclo (A dipende da Uptime e PlannedProduction; Uptime dipende da PlannedProduction)
    /// - Riutilizzo valore già calcolato nello stesso pass senza eccezione
    /// - Versioning solo per valori con formula (i PARAM non vengono chiusi)
    /// </summary>
    public double ComputeRecursiveForce(string valueCode, string contextCode, DateTime t,
                                        string? unitOut = null, string quality = "ok",
                                        string? sourceRunId = null,
                                        HashSet<string>? path = null,
                                        Dictionary<string, double>? memo = null)
    {
        path ??= new HashSet<string>(StringComparer.Ordinal);
        memo ??= new Dictionary<string, double>(StringComparer.Ordinal);

        // Memoization: se già calcolato in questo pass restituisci
        if (memo.TryGetValue(valueCode, out var cached))
            return cached;

        // Se il nodo è già nel path attuale è un vero ciclo strutturale (dovrebbe essere stato impedito da HasCycle())
        if (path.Contains(valueCode))
            throw new Exception($"Dipendenza ciclica reale rilevata in ComputeRecursiveForce per '{valueCode}'");

        var v = FindValueAt(valueCode, t) ?? throw new Exception($"Value {valueCode} @ {t} non trovato");
        var c = FindContextAt(contextCode, t) ?? throw new Exception($"Context {contextCode} @ {t} non trovato");
        var formula = FindFormulaForValueAt(v.Id, t); // può essere null per PARAM

        // Leaf (PARAM): restituisce semplicemente il fact corrente
        if (formula == null)
        {
            var existingLeaf = Facts.Where(f => f.ValueId == v.Id && f.ContextId == c.Id &&
                                                f.ValidFrom <= t && (f.ValidTo == null || t < f.ValidTo))
                                    .OrderByDescending(f => f.ValidFrom).FirstOrDefault()
                               ?? throw new Exception($"Valore PARAM '{valueCode}' mancante (nessuna formula e nessun fact).");
            memo[valueCode] = existingLeaf.DoubleValue;
            return existingLeaf.DoubleValue;
        }

        // Discesa
        path.Add(valueCode);

        // Calcola prima (o ricalcola forzato) tutti gli input
        foreach (var fi in InputsOf(formula.Id))
        {
            var inputCode = Values.First(x => x.Id == fi.InputValueId).ValueCode;
            _ = ComputeRecursiveForce(inputCode, contextCode, t,
                                      unitOut: null, quality: quality,
                                      sourceRunId: sourceRunId, path: path, memo: memo);
        }

        path.Remove(valueCode);

        // Versioning: chiudi il fact precedente solo se esiste formula (qui true)
        var existing = Facts.Where(f => f.ValueId == v.Id && f.ContextId == c.Id &&
                                        f.ValidFrom <= t && (f.ValidTo == null || t < f.ValidTo))
                            .OrderByDescending(f => f.ValidFrom).FirstOrDefault();
        if (existing != null)
            existing.ValidTo = t; // se vuoi evitare record ogni sample, commenta questa linea

        var result = RecomputeAtTime(valueCode, contextCode, t, unitOut, quality, sourceRunId);
        memo[valueCode] = result;
        return result;
    }
}