using KpiApp;
using KpiLib;

var db = new KpiDb();

OEE.Seed(db);

// Demo parameters (simulate external ingest)
var now = DateTime.UtcNow;

db.UpsertIfChangedByCodes("ShiftLength", "LINEA_A", "min", 480, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("BreakTime", "LINEA_A", "min", 30, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("PlannedStop", "LINEA_A", "min", 10, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("UnplannedStop", "LINEA_A", "min", 25, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("SetupTime", "LINEA_A", "min", 15, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("RunTime", "LINEA_A", "min", 420, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("IdealCycleTime", "LINEA_A", "s", 2.40, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("TotalPieces", "LINEA_A", "pcs", 10000, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("GoodPieces", "LINEA_A", "pcs", 9800, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("Rework", "LINEA_A", "pcs", 150, "external", "ingest", "demo", now);
db.UpsertIfChangedByCodes("FAILURE_COUNT", "LINEA_A", "count", 4, "external", "ingest", "demo", now);

// Compute recursively: engine auto-calcola A,P,Q,… e scrive
var oee = db.ComputeRecursive("OEE", "LINEA_A", now, unitOut: "%", quality: "ok", sourceRunId: "calc#demo");
var teep = db.ComputeRecursive("TEEP", "LINEA_A", now, unitOut: "%", quality: "ok", sourceRunId: "calc#demo");
var ooe = db.ComputeRecursive("OOE", "LINEA_A", now, unitOut: "%", quality: "ok", sourceRunId: "calc#demo");

Console.WriteLine($"OEE (0..1): {oee:F6}");
Console.WriteLine($"TEEP (0..1): {teep:F6}");
Console.WriteLine($"OOE  (0..1): {ooe:F6}");
Console.WriteLine();

// Print current facts snapshot
var ctx = db.FindContextAt("LINEA_A", now)!.Id;
var snapshot = db.Facts.Where(f => f.ContextId == ctx && f.ValidTo == null)
                       .Join(db.Values, f => f.ValueId, v => v.Id, (f, v) => new { f, v })
                       .Join(db.Units, fv => fv.f.UnitId, u => u.Id, (fv, u) => new {
                           fv.v.ValueCode,
                           fv.f.DoubleValue,
                           Unit = u.UnitCode,
                           fv.f.ValidFrom,
                           fv.f.CalcMethod
                       })
                       .OrderBy(x => x.ValueCode);
foreach (var row in snapshot)
    Console.WriteLine($"{row.ValueCode,-22} = {row.DoubleValue,12:F6} {row.Unit}   @ {row.ValidFrom:o}   [{row.CalcMethod}]");

