using System.ComponentModel;

namespace KpiApp;

public static class OeeFormulaMethods
{
    private static double SafeDiv(double num, double den) => den > 0 ? num / den : 0.0;
    private static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);

    [Description("Overall Equipment Effectiveness = A * P * Q (clamped)")]
    public static double OEE(double A, double P, double Q) => Clamp01(A) * Clamp01(P) * Clamp01(Q);

    [Description("Availability = Uptime / PlannedProduction (clamped)")]
    public static double A(double Uptime, double PlannedProduction) => Clamp01(SafeDiv(Uptime, PlannedProduction));

    [Description("Performance = (IdealCycleTime * TotalPieces) / (RunTime * 60) (clamped)")]
    public static double P(double IdealCycleTime, double TotalPieces, double RunTime)
        => Clamp01(SafeDiv(IdealCycleTime * TotalPieces, RunTime * 60.0));

    [Description("Quality = GoodPieces / TotalPieces (clamped)")]
    public static double Q(double GoodPieces, double TotalPieces) => Clamp01(SafeDiv(GoodPieces, TotalPieces));

    [Description("Projected Performance using estimated pieces (clamped)")]
    public static double ProjectedPerformance(double IdealCycleTime, double EstTotalPieces, double RunTime)
        => Clamp01(SafeDiv(IdealCycleTime * EstTotalPieces, RunTime * 60.0));

    [Description("Projected OEE = A * ProjectedPerformance * Q (clamped)")]
    public static double ProjectedOEE(double A, double ProjectedPerformance, double Q)
        => Clamp01(A) * Clamp01(ProjectedPerformance) * Clamp01(Q);

    public static double PlannedProduction(double ShiftLength, double BreakTime, double PlannedStop)
        => ShiftLength - BreakTime - PlannedStop;

    public static double Uptime(double PlannedProduction, double UnplannedStop, double SetupTime)
        => PlannedProduction - UnplannedStop - SetupTime;

    public static double Downtime(double ShiftLength, double Uptime) => ShiftLength - Uptime;
    public static double TotalTime(double ShiftLength) => ShiftLength;

    public static double ActualRate(double TotalPieces, double RunTime)
        => SafeDiv(TotalPieces, RunTime / 60.0);

    public static double CycleTime(double RunTime, double TotalPieces)
        => SafeDiv(RunTime * 60.0, TotalPieces);

    public static double PerformanceLoss(double RunTime, double IdealCycleTime, double TotalPieces)
        => RunTime - SafeDiv(IdealCycleTime * TotalPieces, 60.0);

    public static double QualityLoss(double TotalPieces, double GoodPieces, double IdealCycleTime)
        => SafeDiv((TotalPieces - GoodPieces) * IdealCycleTime, 60.0);

    public static double AvailabilityLoss(double UnplannedStop, double SetupTime) => UnplannedStop + SetupTime;

    public static double RateLoss(double TargetRate, double ActualRate) => TargetRate - ActualRate;

    public static double Throughput(double GoodPieces, double RunTime)
        => SafeDiv(GoodPieces, RunTime / 60.0);

    public static double Yield(double GoodPieces, double TotalPieces) => Clamp01(SafeDiv(GoodPieces, TotalPieces));

    public static double FirstPassYield(double GoodPieces, double Rework)
        => Clamp01(SafeDiv(GoodPieces, GoodPieces + Rework));

    public static double Utilization(double Uptime, double ShiftLength) => Clamp01(SafeDiv(Uptime, ShiftLength));

    public static double Load(double PlannedProduction, double ShiftLength) => Clamp01(SafeDiv(PlannedProduction, ShiftLength));

    public static double CycleEfficiency(double IdealCycleTime, double GoodPieces, double RunTime)
        => Clamp01(SafeDiv(IdealCycleTime * GoodPieces, RunTime * 60.0));

    public static double MeanTime(double RunTime, double TotalPieces)
        => SafeDiv(RunTime * 60.0, TotalPieces);

    public static double MTBF(double Uptime, double FAILURE_COUNT)
        => SafeDiv(Uptime, FAILURE_COUNT);

    public static double MTTR(double UnplannedStop, double FAILURE_COUNT)
        => SafeDiv(UnplannedStop, FAILURE_COUNT);

    public static double ScrapCount(double TotalPieces, double GoodPieces) => TotalPieces - GoodPieces;

    public static double ScrapRate(double TotalPieces, double GoodPieces)
        => Clamp01(SafeDiv(TotalPieces - GoodPieces, TotalPieces));

    public static double ReworkRate(double Rework, double GoodPieces)
        => Clamp01(SafeDiv(Rework, GoodPieces + Rework));

    public static double RateAttainment(double ActualRate, double TargetRate)
        => Clamp01(SafeDiv(ActualRate, TargetRate));

    public static double ProductionAttainment(double GoodPieces, double TargetRate, double RunTime)
        => Clamp01(SafeDiv(GoodPieces, TargetRate * (RunTime / 60.0)));

    public static double TEEP(double OEE, double Load) => Clamp01(Clamp01(OEE) * Clamp01(Load));
    public static double OOE(double OEE, double Utilization) => Clamp01(Clamp01(OEE) * Clamp01(Utilization));

    public static double YieldLoss(double Yield) => 1.0 - Clamp01(Yield);
    public static double AvailabilityLossPct(double A) => 1.0 - Clamp01(A);
    public static double PerformanceLossPct(double P) => 1.0 - Clamp01(P);
    public static double QualityLossPct(double Q) => 1.0 - Clamp01(Q);
}