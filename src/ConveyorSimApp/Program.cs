using ConveyorSimApp.Models;   // throttle timing
using ConveyorSimApp.OpcUa;
using InductionMotorSimLib;
using IndustrialSimLib;
using IndustrialSimLib.SimEvents;
using KpiApp;
using KpiLib;
using Opc.Ua;
using OpcUaServerLib;
using PackageSimLib;
using System.Diagnostics;
using System.Globalization;
using ThreePhaseSupplySimLib;
using VfdSimLib;

var db = new KpiDb();
OEE.Seed(db);

// ---------- KPI runtime aggregation (replaces static parameter paste) ----------
var baseNow = DateTime.UtcNow;

// Configurable shift meta (could be args/env in future)
double shiftLengthMin = 480;     // 8h shift
double plannedBreakMin = 30;
double plannedStopMin = 10;

// Runtime accumulators (seconds)
double setupTimeSec = 0.0;
double runTimeSec = 0.0;
double unplannedStopSec = 0.0;

// Piece counters
long totalPiecesOut = 0;
long goodPiecesOut = 0;     // all good (no scrap logic yet)
long reworkPieces = 0;
long failureCount = 0;
double lastKpiSimTime = 0.0;

// Thresholds to mark setup completed (avg freq reaches ≥ 90% of target once)
bool setupCompleted = false;
double setupFreqThresholdFactor = 0.90;

// Track previous running state to count failures (trip transitions)
bool prevRunning = true;

// Seed STATIC-like params that won’t change (or rarely)
db.UpsertIfChangedByCodes("ShiftLength", "LINEA_A", "min", shiftLengthMin, "ok", "config", "init", baseNow);
db.UpsertIfChangedByCodes("BreakTime", "LINEA_A", "min", plannedBreakMin, "ok", "config", "init", baseNow);
db.UpsertIfChangedByCodes("PlannedStop", "LINEA_A", "min", plannedStopMin, "ok", "config", "init", baseNow);
// Ideal cycle time (design)
db.UpsertIfChangedByCodes("IdealCycleTime", "LINEA_A", "s", 2.40, "ok", "config", "init", baseNow);

// ----------------------------
// Conveyor configuration
// ----------------------------
int Segments = 5;
double ConveyorLengthM = 50.0;
double SegmentLengthM = ConveyorLengthM / Segments;

// Mechanics per segment (motor -> gearbox -> pulley)
// motor RPM -> belt speed: v = (2π * rpm / 60) * (PulleyRadius / GearRatio)
double PulleyRadiusM = 0.15;
double GearRatio = 12.0;
double MechEfficiency = 0.9;
double MuRoll = 0.03;

// Package generation
double packageSpawnPeriod = 1.0;
double nextSpawn = 0.0;

// ----------------------------
// Supply (grid)
// ----------------------------
var supplySettings = new ThreePhaseSupplySettings
{
    NominalVoltageLL = 400.0,
    NominalFrequency = 50.0,
    VoltageSlewRate = 1000.0,
    FrequencySlewRate = 10.0,
    UnderVoltPU = 0.50,
    OverVoltPU = 1.25
};
var supplyState = new ThreePhaseSupplyState();
supplyState.TargetVoltageLL.Set(supplySettings.NominalVoltageLL);
supplyState.TargetFrequency.Set(supplySettings.NominalFrequency);
var supplyInputs = new ThreePhaseSupplyInputs();
var supplyOutputs = new ThreePhaseSupplyOutputs();
supplyOutputs.LineLineVoltage.Set(supplySettings.NominalVoltageLL);
supplyOutputs.Frequency.Set(supplySettings.NominalFrequency);
var supply = new ThreePhaseSupply(supplySettings, supplyState, supplyInputs, supplyOutputs);

// ----------------------------
// Segment devices
// ----------------------------
var vfdSettings = new VfdSettings
{
    RatedVoltageLL = 400.0,
    RatedFrequency = 50.0,
    MaxCurrent = 30.0,
    Accel = 15.0,
    Decel = 15.0,
    VoltBoost = 0.06,
    ThermalTimeConstant = 40.0,
    MaxHeatsinkTemp = 85.0,
    AmbientTemp = 25.0,
    OverCurrentMultiple = 1.6,
    UnderVoltPUNomDC = 0.55,
    OverVoltPUNomDC = 1.20
};
double vfdTargetFreqHz = 30.0;

var segments = new Segment[Segments];
for (int i = 0; i < segments.Length; i++)
{
    var vfdState = new VfdState();
    vfdState.VdcNom.Set(Math.Sqrt(2.0) * vfdSettings.RatedVoltageLL);
    vfdState.BusVoltage.Set(Math.Sqrt(2.0) * vfdSettings.RatedVoltageLL);
    vfdState.HeatsinkTemp.Set(vfdSettings.AmbientTemp);
    vfdState.TargetFrequency.Set(vfdTargetFreqHz);

    var vfdInputs = new VfdInputs();
    var vfdOutputs = new VfdOutputs();
    var vfd = new Vfd(vfdSettings, vfdState, vfdInputs, vfdOutputs);

    var motorSettings = new InductionMotorSettings
    {
        RatedVoltageLL = 400.0,
        RatedFrequency = 50.0,
        PolePairs = 2,
        RatedPower = 4000.0,
        RatedSpeedRpm = 1440.0,
        Inertia = 0.15,
        ViscFriction = 0.002,
        CoulombFriction = 0.8,
        SlipNom = 0.03,
        TorqueMaxPU = 2.0,
        Inom = 12.0,
        ConstLoadTorque = 3.0,
        JamExtraTorque = 150.0,
        BearingExtraTorque = 5.0
    };
    var omegaRated = 2.0 * Math.PI * (motorSettings.RatedSpeedRpm / 60.0);
    var motorState = new InductionMotorState();
    motorState.SpeedRpm.Reset();
    motorState.VratedPhPh.Set(motorSettings.RatedVoltageLL);
    motorState.Trated.Set(motorSettings.RatedPower / omegaRated);
    var motorInputs = new InductionMotorInputs();
    var motorOutputs = new InductionMotorOutputs();
    var motor = new InductionMotor(motorSettings, motorState, motorInputs, motorOutputs);

    segments[i] = new Segment
    {
        Index = i,
        StartM = i * SegmentLengthM,
        EndM = (i + 1) * SegmentLengthM,
        Vfd = vfd, VfdState = vfdState, VfdInputs = vfdInputs, VfdOutputs = vfdOutputs,
        Motor = motor, MotorState = motorState, MotorSettings = motorSettings,
        MotorInputs = motorInputs, MotorOutputs = motorOutputs,
        BaseConstTorque = motorSettings.ConstLoadTorque,
        LastBeltSpeed = 0.0
    };
}

// ----------------------------
// Packages
// ----------------------------
var packages = new List<Package>();

// ----------------------------
// Scenario
// ----------------------------
SimEvent[] scenario = [
    new ToggleAnomalyActionEvent( 5.0, (state) => segments[2].MotorState.An_LoadJam.True() ),
    // At 10s REMOVE the jam instead of setting again (previously re-enabled)
    new ToggleAnomalyActionEvent(10.0, (state) => segments[2].MotorState.An_LoadJam.False() ),
    new ToggleAnomalyActionEvent(15.0, (state) => supplyState.An_UnderVoltage.True() ),
    new ToggleAnomalyActionEvent(16.5, (state) => supplyState.An_UnderVoltage.False() ),
];

// ----------------------------
// OPC UA server
// ----------------------------
ConveyorSimApp.OpcUa.MyNodeManager ns = default;
var opc = await MyOpcUaServerHost.StartAsync("ConveyorSim", "opc.tcp://localhost:4840/ConveyorSim",
    createNodeManager: (srv, conf) =>
    {
        ns = new MyNodeManager(srv, conf, "Conveyor", "urn:ConveyorSim:NodeManager", BuildConveyorNamespace);
        ns.SystemContext.NodeIdFactory = ns;
        return ns;
    });

// ----------------------------
// Simulation timing
// ----------------------------
double totalTimeSec = 600.0;
double dt = 0.01;
double samplePeriod = 0.5;
double speedFactor = 1; //  0.5;
double startDelaySec = 2.0;

if (args.Length > 0 && double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var argDur)) totalTimeSec = argDur;
if (args.Length > 1 && double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var argSpeed)) speedFactor = Math.Max(1e-3, argSpeed);
if (args.Length > 2 && double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var argDelay)) startDelaySec = Math.Max(0.0, argDelay);

if (startDelaySec > 0) await Task.Delay(TimeSpan.FromSeconds(startDelaySec));

var sw = Stopwatch.StartNew();
var nextWall = sw.Elapsed;

// Run loop
double nextSample = 0.0;
int idx = 0;
var simState = new SimState((string key, bool enable) => { });

PrintHeader();
while (simState.Time < totalTimeSec)
{
    while (idx < scenario.Length && scenario[idx].Time <= simState.Time + 1e-9)
    {
        scenario[idx].Apply(simState);
        idx++;
    }

    // ---- Package spawn (replace block inside the run loop) ----
    if (simState.Time >= nextSpawn)
    {
        var package = new Package();
        package.PositionM.Reset();
        // Spawn near the beginning so it must traverse all segments
        package.PositionM.Set(0.5 + Random.Shared.NextDouble() * 1.0); // 0.5 .. 1.5 m
        packages.Add(package);
        nextSpawn += packageSpawnPeriod;
    }

    simState.Step(dt);

    supply.Step(dt, simState);
    UpdateBindables(supplyState, supplyOutputs);

    // Segments
    for (int i = 0; i < Segments; i++)
    {
        var seg = segments[i];

        seg.VfdInputs.SupplyVoltageLL.Set(supplyOutputs.LineLineVoltage);
        seg.VfdInputs.SupplyFrequency.Set(supplyOutputs.Frequency);

        seg.Vfd.Step(dt, simState);

        seg.MotorInputs.DriveFrequencyCmd.Set(seg.VfdOutputs.OutputFrequency);
        seg.MotorInputs.DriveVoltageCmd.Set(seg.VfdOutputs.OutputVoltage);

        double beltSpeed = BeltSpeedFromRpm(seg.MotorState.SpeedRpm);
        double dv = (beltSpeed - seg.LastBeltSpeed) / dt;
        seg.LastBeltSpeed = beltSpeed;

        double segmentMass = 0.0;
        for (int p = 0; p < packages.Count; p++)
        {
            var pkg = packages[p];
            if (pkg.PositionM >= seg.StartM && pkg.PositionM < seg.EndM)
                segmentMass += pkg.MassKg;
        }

        double F = segmentMass * 9.81 * MuRoll + segmentMass * dv;
        double T_load_pkg = (F * PulleyRadiusM) / (GearRatio * MechEfficiency);
        seg.MotorSettings.ConstLoadTorque = seg.BaseConstTorque + T_load_pkg;

        seg.Motor.Step(dt, simState);

        seg.VfdInputs.MotorCurrentFeedback.Set(seg.MotorOutputs.PhaseCurrent);
        seg.Vfd.Step2(dt, simState);

        UpdateBindables(seg.VfdState, seg.VfdInputs, seg.VfdOutputs, seg.MotorState, seg.MotorInputs, seg.MotorOutputs);
    }

    // Packages movement
    for (int p = packages.Count - 1; p >= 0; p--)
    {
        var pkg = packages[p];
        int segIdx = Math.Clamp((int)Math.Floor(pkg.PositionM / SegmentLengthM), 0, Segments - 1);
        double beltSpeed = BeltSpeedFromRpm(segments[segIdx].MotorState.SpeedRpm);
        pkg.PositionM.Add(beltSpeed * dt);

        if (pkg.PositionM >= ConveyorLengthM)
        {
            packages.RemoveAt(p);
            totalPiecesOut++;
            goodPiecesOut++; // restore: all pieces considered good for now
        }
    }

    UpdatePackages(packages);

    if (simState.Time >= nextSample)
    {
        PrintStatus(simState.Time, segments, packages);

        // KPI update & compute at sample boundary
        var simNow = baseNow.AddSeconds(simState.Time);
        double kpiDt = simState.Time - lastKpiSimTime;            // real elapsed simulated seconds since last KPI update
        if (kpiDt < 0) kpiDt = 0;                                 // safety
        UpdateKpiParameters(simNow, kpiDt, segments, simState);   // pass correct elapsed time, NOT fixed integration dt
        lastKpiSimTime = simState.Time;

        nextSample += samplePeriod;
    }

    nextWall += TimeSpan.FromSeconds(dt / Math.Max(1e-6, speedFactor));
    var delay = nextWall - sw.Elapsed;
    if (delay > TimeSpan.Zero)
        await Task.Delay(delay);
}

Console.WriteLine();
Console.WriteLine("Event log:");
foreach (var e in simState.EventLog) Console.WriteLine(" - " + e);

await opc.StopAsync();

// Helpers & types unchanged below...
double BeltSpeedFromRpm(double rpm) => (2.0 * Math.PI * rpm / 60.0) * (PulleyRadiusM / GearRatio);

void PrintHeader()
{
    Console.WriteLine("time    seg  f_out(Hz)  V_out(V)   I(A)   rpm    v_belt(m/s)  pkgs  T_hs(°C)  Vdc(V)  RUN Trip");
    Console.WriteLine(new string('-', 110));
}

void PrintStatus(double t, Segment[] segs, List<Package> pkgs)
{
    for (int i = 0; i < segs.Length; i++)
    {
        var s = segs[i];
        double v = BeltSpeedFromRpm(s.MotorState.SpeedRpm);
        int count = 0;
        for (int p = 0; p < pkgs.Count; p++)
            if (pkgs[p].PositionM >= s.StartM && pkgs[p].PositionM < s.EndM) count++;

        Console.WriteLine($"{t,6:F2}  {i,3}  {s.VfdOutputs.OutputFrequency,8:F1}  {s.VfdOutputs.OutputVoltage,8:F0}  {s.MotorOutputs.PhaseCurrent,5:F1}  {s.MotorState.SpeedRpm,6:F0}  {v,10:F2}  {count,4}  {s.VfdState.HeatsinkTemp,7:F1}  {s.VfdState.BusVoltage,6:F0}  {(s.Running ? "Y" : "N")}   {s.Trip}");
    }

    // Show first few packages with position and mass
    int shown = Math.Min(5, pkgs.Count);
    if (shown > 0)
    {
        Console.Write("       pkgs: ");
        for (int i = 0; i < shown; i++)
            Console.Write($"[{pkgs[i].PositionM,5:F1} m, {pkgs[i].MassKg,5:F2} kg] ");
        Console.WriteLine();
    }
}

void UpdateBindables(params object[] values)
{
    foreach (var value in values)
    {
        var type = value.GetType();
        foreach (var prop in type.GetProperties())
        {
            if (prop.PropertyType == typeof(DoubleBindable))
            {
                var dbv = (DoubleBindable)prop.GetValue(value);
                ns.UpdateDoubleBindable(dbv);
            }
            else if (prop.PropertyType == typeof(BoolBindable))
            {
                var bb = (BoolBindable)prop.GetValue(value);
                ns.UpdateBoolBindable(bb);
            }
        }
    }
}

void UpdatePackages(IReadOnlyList<Package> pkgs)
{
    var positions = pkgs.Select(p => p.PositionM).ToArray();
    var masses = pkgs.Select(p => p.MassKg).ToArray();
}

void BuildConveyorNamespace(NodeState rootNode)
{
    var supplyFolder = rootNode.AddFolder("Supply");
    supplyFolder.AddVar(supplyOutputs, xx => xx.LineLineVoltage);
    supplyFolder.AddVar(supplyOutputs, xx => xx.Frequency);
    supplyFolder.AddVar(supplyState, xx => xx.TargetVoltageLL);
    supplyFolder.AddVar(supplyState, xx => xx.TargetFrequency);
    supplyFolder.AddVar(supplyState, xx => xx.An_UnderVoltage);
    supplyFolder.AddVar(supplyState, xx => xx.An_OverVoltage);
    supplyFolder.AddVar(supplyState, xx => xx.An_FrequencyDrift);

    var segmentsFolder = rootNode.AddFolder("Segments");
    for (int i = 0; i < segments.Length; i++)
    {
        var segmentObject = segments[i];
        var seg = segmentsFolder.AddFolder($"Segment[{i}]");
        var vfd = seg.AddFolder("Vfd");
        var vfdState = vfd.AddFolder("State");
        var vfdIn = vfd.AddFolder("Inputs");
        var vfdOut = vfd.AddFolder("Outputs");

        var mot = seg.AddFolder("Motor");
        var motState = mot.AddFolder("State");
        var motIn = mot.AddFolder("Inputs");
        var motOut = mot.AddFolder("Outputs");

        vfdState.AddVar(segmentObject.VfdState, xx => xx.TargetFrequency);
        vfdState.AddVar(segmentObject.VfdState, xx => xx.BusVoltage);
        vfdState.AddVar(segmentObject.VfdState, xx => xx.HeatsinkTemp);
        vfdState.AddVar(segmentObject.VfdState, xx => xx.VdcNom);
        vfdState.AddVar(segmentObject.VfdState, xx => xx.An_UnderVoltage);
        vfdState.AddVar(segmentObject.VfdState, xx => xx.An_OverVoltage);
        vfdState.AddVar(segmentObject.VfdState, xx => xx.An_PhaseLoss);
        vfdState.AddVar(segmentObject.VfdState, xx => xx.An_GroundFault);

        vfdIn.AddVar(segmentObject.VfdInputs, xx => xx.SupplyVoltageLL);
        vfdIn.AddVar(segmentObject.VfdInputs, xx => xx.SupplyFrequency);
        vfdIn.AddVar(segmentObject.VfdInputs, xx => xx.MotorCurrentFeedback);

        vfdOut.AddVar(segmentObject.VfdOutputs, xx => xx.OutputFrequency);
        vfdOut.AddVar(segmentObject.VfdOutputs, xx => xx.OutputVoltage);

        motState.AddVar(segmentObject.MotorState, xx => xx.SpeedRpm);
        motState.AddVar(segmentObject.MotorState, xx => xx.ElectTorque);
        motState.AddVar(segmentObject.MotorState, xx => xx.Trated);
        motState.AddVar(segmentObject.MotorState, xx => xx.VratedPhPh);
        motState.AddVar(segmentObject.MotorState, xx => xx.An_PhaseLoss);
        motState.AddVar(segmentObject.MotorState, xx => xx.An_LoadJam);
        motState.AddVar(segmentObject.MotorState, xx => xx.An_BearingWear);
        motState.AddVar(segmentObject.MotorState, xx => xx.An_SensorNoise);

        motIn.AddVar(segmentObject.MotorInputs, xx => xx.DriveFrequencyCmd);
        motIn.AddVar(segmentObject.MotorInputs, xx => xx.DriveVoltageCmd);

        motOut.AddVar(segmentObject.MotorOutputs, xx => xx.PhaseCurrent);
    }

    var pkgsFolder = rootNode.AddFolder("Packages");
    pkgsFolder.AddVar<int>("Count", DataTypeIds.Int32);
    pkgsFolder.AddArrayVar<double>("Positions");
    pkgsFolder.AddArrayVar<double>("Masses");
}

// Dynamic parameter updater
int PackagesInSegment(int segIndex)
{
    double start = segments[segIndex].StartM;
    double end = segments[segIndex].EndM;
    int count = 0;
    for (int i = 0; i < packages.Count; i++)
    {
        var pos = packages[i].PositionM.Value;
        if (pos >= start && pos < end)
            count++;
    }
    return count;
}

// 2) Add this helper (place it near other helpers, e.g. before UpdateKpiParameters)
void DumpKpiInputs(DateTime simNow)
{
    string[] codes = { "IdealCycleTime", "RunTime", "TotalPieces", "GoodPieces", "UnplannedStop", "SetupTime" };
    var ctx = db.FindContextAt("LINEA_A", simNow)!;
    Console.WriteLine("   KPI INPUT SNAPSHOT:");
    foreach (var code in codes)
    {
        var v = db.FindValueAt(code, simNow);
        if (v == null)
        {
            Console.WriteLine($"      {code} = (value_dim missing)");
            continue;
        }
        var fact = db.Facts
            .Where(f => f.ValueId == v.Id && f.ContextId == ctx.Id &&
                        f.ValidFrom <= simNow && (f.ValidTo == null || simNow < f.ValidTo))
            .OrderByDescending(f => f.ValidFrom)
            .FirstOrDefault();
        if (fact == null)
            Console.WriteLine($"      {code} = (no fact)");
        else
            Console.WriteLine($"      {code,-15} = {fact.DoubleValue,10:F6} (unitId={fact.UnitId})");
    }
}

// 3) REPLACE UpdateKpiParameters with this enhanced version
void UpdateKpiParameters(DateTime simNow, double dt, Segment[] segs, SimState simState)
{
    // 1. Detect setup completion
    if (!setupCompleted)
    {
        double avgFreq = segs.Average(s => (double)s.VfdOutputs.OutputFrequency);
        double target = segs.Average(s => (double)s.VfdState.TargetFrequency);
        if (target > 1e-6 && avgFreq >= setupFreqThresholdFactor * target)
            setupCompleted = true;
    }

    // 2. Production / blockage state
    var activeMask = segs.Select(s => (double)s.VfdOutputs.OutputFrequency > 0.5).ToArray();
    bool blocked = false;
    for (int i = 0; i < segs.Length; i++)
    {
        if (!activeMask[i])
        {
            int pkgHere = PackagesInSegment(i);
            if (pkgHere > 0)
            {
                bool upstreamActive = activeMask.Take(i).Any(a => a);
                if (upstreamActive || i == 0)
                {
                    blocked = true;
                    break;
                }
            }
        }
    }

    // 3. Time classification
    if (simState.Running)
    {
        if (!setupCompleted)
            setupTimeSec += dt;
        else if (blocked)
            unplannedStopSec += dt;
        else if (activeMask.Any(a => a))
            runTimeSec += dt;
        else
            unplannedStopSec += dt;
    }
    else
    {
        if (setupCompleted)
            unplannedStopSec += dt;
        else
            setupTimeSec += dt;
    }

    // 4. Failure transitions
    if (prevRunning && !simState.Running)
        failureCount++;
    prevRunning = simState.Running;

    // 5. Synchronize GoodPieces with TotalPieces (no scrap yet)
    if (goodPiecesOut != totalPiecesOut)
        goodPiecesOut = totalPiecesOut;

    // 6. Upsert evolving PARAMs
    db.UpsertIfChangedByCodes("SetupTime",     "LINEA_A", "min", setupTimeSec     / 60.0, "ok", "derived", "sim", simNow);
    db.UpsertIfChangedByCodes("RunTime",       "LINEA_A", "min", runTimeSec       / 60.0, "ok", "derived", "sim", simNow);
    db.UpsertIfChangedByCodes("UnplannedStop", "LINEA_A", "min", unplannedStopSec / 60.0, "ok", "derived", "sim", simNow);
    db.UpsertIfChangedByCodes("TotalPieces",   "LINEA_A", "pcs", totalPiecesOut,         "ok", "derived", "sim", simNow);
    db.UpsertIfChangedByCodes("GoodPieces",    "LINEA_A", "pcs", goodPiecesOut,          "ok", "derived", "sim", simNow);
    db.UpsertIfChangedByCodes("Rework",        "LINEA_A", "pcs", reworkPieces,           "ok", "derived", "sim", simNow);
    db.UpsertIfChangedByCodes("FAILURE_COUNT", "LINEA_A", "count", failureCount,         "ok", "derived", "sim", simNow);

    // 7. Debug snapshot of raw inputs
    DumpKpiInputs(simNow);

    // 8. Warm-up skip (optional)
    if (totalPiecesOut == 0 && runTimeSec < 30.0)
    {
        Console.WriteLine($"[KPI t={simState.Time,6:F1}s] (warming) Pieces=0 Run(min)={runTimeSec/60.0:F3}");
        return;
    }

    // 9. Compute KPIs (FORCED recompute each sample to avoid stale zeros)
    double avail = db.ComputeRecursiveForce("A",    "LINEA_A", simNow, "ratio", "ok", "sim");
    double perf  = db.ComputeRecursiveForce("P",    "LINEA_A", simNow, "ratio", "ok", "sim");
    double qual  = db.ComputeRecursiveForce("Q",    "LINEA_A", simNow, "ratio", "ok", "sim");
    double oee   = db.ComputeRecursiveForce("OEE",  "LINEA_A", simNow, "ratio", "ok", "sim");
    double teep  = db.ComputeRecursiveForce("TEEP", "LINEA_A", simNow, "ratio", "ok", "sim");
    double ooeVal= db.ComputeRecursiveForce("OOE",  "LINEA_A", simNow, "ratio", "ok", "sim");

    Console.WriteLine(
        $"[KPI t={simState.Time,6:F1}s] OEE={oee*100:F2}%  A={avail*100:F2}%  P={perf*100:F2}%  Q={qual*100:F2}%  " +
        $"TEEP={teep*100:F2}%  OOE={ooeVal*100:F2}%  Run(min)={runTimeSec/60.0:F3}  Unpl(min)={unplannedStopSec/60.0:F3} " +
        $"TotPieces={totalPiecesOut} GoodPieces={goodPiecesOut} Blocked={(blocked ? "Y" : "N")}");
}
