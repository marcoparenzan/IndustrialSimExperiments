using KpiLib;
using System.Reflection;

namespace KpiApp;

public static class OEE
{
    public static void Seed(KpiDb db)
    {
        var t = new DateTime(2025, 1, 1);

        // Units
        db.EnsureUnit("ratio", "Dimensionless ratio (0..1)", t);
        db.EnsureUnit("%", "Percent (0..100)", t);
        db.EnsureUnit("pcs", "Pieces", t);
        db.EnsureUnit("pcs/h", "Pieces per hour", t);
        db.EnsureUnit("min", "Minutes", t);
        db.EnsureUnit("h", "Hours", t);
        db.EnsureUnit("s", "Seconds", t);
        db.EnsureUnit("count", "Count", t);

        // Conversions
        db.EnsureConversion("s", "min", t, 1.0 / 60.0, 0);
        db.EnsureConversion("min", "s", t, 60.0, 0);
        db.EnsureConversion("h", "min", t, 60.0, 0);
        db.EnsureConversion("min", "h", t, 1.0 / 60.0, 0);
        db.EnsureConversion("ratio", "%", t, 100.0, 0);
        db.EnsureConversion("%", "ratio", t, 0.01, 0);

        // Context
        db.EnsureContext("LINEA_A", "Linea A", "PLANT1", t);

        // PARAM values
        var paramList = new (string code, string name, string unit, string cat)[]
        {
            ("ShiftLength","Shift Length","min","Planning"),
            ("BreakTime","Break Time","min","Planning"),
            ("PlannedStop","Planned Stop","min","Planning"),
            ("UnplannedStop","Unplanned Stop","min","Losses"),
            ("SetupTime","Setup Time","min","Losses"),
            ("RunTime","Run Time","min","Execution"),
            ("GoodPieces","Good Pieces","pcs","Quality"),
            ("TotalPieces","Total Pieces","pcs","Quality"),
            ("IdealCycleTime","Ideal Cycle Time (s/pc)","s","Performance"),
            ("TargetRate","Target Rate (pcs/h)","pcs/h","Performance"),
            ("Rework","Rework Pieces","pcs","Quality"),
            ("FAILURE_COUNT","Number of Failures","count","Reliability")
        };
        foreach (var p in paramList)
            db.EnsureValue(p.code, p.name, "PARAM", p.unit, p.cat, t);
        db.EnsureValue("EstTotalPieces", "Estimated Total Pieces", "PARAM", "pcs", "Derived", t);

        // KPI (dimensionless -> ratio)
        var kpiList = new (string code, string name, string unit, string cat)[]
        {
            ("OEE","Overall Equipment Effectiveness","ratio","OEE"),
            ("A","Availability","ratio","OEE"),
            ("P","Performance","ratio","OEE"),
            ("Q","Quality","ratio","OEE"),
            ("Uptime","Operating Time","min","Time"),
            ("Downtime","Downtime","min","Time"),
            ("PlannedProduction","Planned Production Time","min","Time"),
            ("TotalTime","Total Time","min","Time"),
            ("ActualRate","Actual Rate","pcs/h","Rate"),
            ("CycleTime","Actual Cycle Time (s/pc)","s","Rate"),
            ("PerformanceLoss","Performance Loss (min)","min","Losses"),
            ("QualityLoss","Quality Loss (min)","min","Losses"),
            ("AvailabilityLoss","Availability Loss (min)","min","Losses"),
            ("RateLoss","Rate Loss (pcs/h)","pcs/h","Rate"),
            ("Throughput","Throughput (good pcs/h)","pcs/h","Rate"),
            ("Yield","Yield","ratio","Quality"),
            ("FirstPassYield","First Pass Yield","ratio","Quality"),
            ("Utilization","Utilization","ratio","Capacity"),
            ("Load","Load","ratio","Capacity"),
            ("CycleEfficiency","Cycle Efficiency","ratio","Performance"),
            ("MeanTime","Mean Time per Unit (s/pc)","s","Performance"),
            ("MTBF","Mean Time Between Failures","min","Reliability"),
            ("MTTR","Mean Time To Repair","min","Reliability"),
            ("ScrapCount","Scrap Count (defective pcs)","pcs","Quality"),
            ("ScrapRate","Scrap Rate","ratio","Quality"),
            ("ReworkRate","Rework Rate","ratio","Quality"),
            ("RateAttainment","Rate Attainment","ratio","Rate"),
            ("ProductionAttainment","Production Attainment","ratio","Rate"),
            ("TEEP","Total Effective Eq. Performance","ratio","OEE"),
            ("OOE","Overall Operations Effectiveness","ratio","OEE"),
            ("YieldLoss","Yield Loss","ratio","Quality"),
            ("AvailabilityLossPct","Availability Loss (1-A)","ratio","OEE"),
            ("PerformanceLossPct","Performance Loss (1-P)","ratio","OEE"),
            ("QualityLossPct","Quality Loss (1-Q)","ratio","OEE"),
            ("ProjectedPerformance","Projected Performance (est)","ratio","OEE"),
            ("ProjectedOEE","Projected OEE (est)","ratio","OEE")
        };
        foreach (var k in kpiList)
            db.EnsureValue(k.code, k.name, "KPI", k.unit, k.cat, t);

        // Direct method mapping helper:
        // Uses method name == target code; parameters must match input value codes.
        void Map(string target)
        {
            var type = typeof(OeeFormulaMethods);
            var mi = type.GetMethod(target, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new InvalidOperationException($"Method {target} not found in {type.FullName}");
            var dict = mi.GetParameters()
                         .ToDictionary(p => p.Name!, p => p.Name!, StringComparer.Ordinal);
            db.EnsureFormulaMethod(target, t, $"{type.FullName}.{target}", target, dict);
        }

        // Map all computed KPI
        string[] computed =
        {
            "OEE","A","P","Q","PlannedProduction","Uptime","Downtime","TotalTime",
            "ActualRate","CycleTime","PerformanceLoss","QualityLoss","AvailabilityLoss",
            "RateLoss","Throughput","Yield","FirstPassYield","Utilization","Load",
            "CycleEfficiency","MeanTime","MTBF","MTTR","ScrapCount","ScrapRate",
            "ReworkRate","RateAttainment","ProductionAttainment","TEEP","OOE",
            "YieldLoss","AvailabilityLossPct","PerformanceLossPct","QualityLossPct",
            "ProjectedPerformance","ProjectedOEE"
        };

        foreach (var code in computed)
            Map(code);
    }
}
