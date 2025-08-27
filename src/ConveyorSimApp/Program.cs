using ConveyorSimApp.Models;   // throttle timing
using ConveyorSimApp.OpcUa; // + add this using
using InductionMotorSimLib;
using IndustrialSimLib;
using IndustrialSimLib.SimEvents;
using Opc.Ua;
using OpcUaServerLib;
using PackageSimLib;
using System.Diagnostics;
using System.Globalization;
using ThreePhaseSupplySimLib;
using VfdSimLib;

// ----------------------------
// Conveyor configuration
// ----------------------------
int Segments = 5;
double ConveyorLengthM = 50.0;
double SegmentLengthM = ConveyorLengthM / Segments;

// Mechanics per segment (motor -> gearbox -> pulley)
// motor RPM -> belt speed: v = (2π * rpm / 60) * (PulleyRadius / GearRatio)
double PulleyRadiusM = 0.15; // 300 mm diameter drum
double GearRatio = 12.0;     // motor:drum speed ratio
double MechEfficiency = 0.9; // mechanical efficiency motor→belt
double MuRoll = 0.03;        // rolling resistance coeff

// Package generation
double packageSpawnPeriod = 1.0; // s
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
    UnderVoltPU = 0.50, // match legacy VFD behavior
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
// Segment devices (5x)
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

// Target belt: ~0.8 m/s → motor electrical freq near 30 Hz (depends on slip and mechanics)
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
        RatedPower = 4000.0,     // ~4 kW per segment
        RatedSpeedRpm = 1440.0,
        Inertia = 0.15,          // includes belt/drum reflected
        ViscFriction = 0.002,
        CoulombFriction = 0.8,
        SlipNom = 0.03,
        TorqueMaxPU = 2.0,
        Inom = 12.0,
        ConstLoadTorque = 3.0,   // base torque without packages
        JamExtraTorque = 150.0,  // jam torque
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
        Motor = motor, MotorState = motorState, MotorSettings = motorSettings, MotorInputs = motorInputs, MotorOutputs = motorOutputs,
        BaseConstTorque = motorSettings.ConstLoadTorque,
        LastBeltSpeed = 0.0
    };
}

// ----------------------------
// Packages
// ----------------------------
var packages = new List<Package>();

// ----------------------------
// Scenario (optional anomalies)
// ----------------------------
SimEvent[] scenario = [
    new ToggleAnomalyActionEvent( 5.0, (state) => segments[2].MotorState.An_LoadJam.True() ),
    new ToggleAnomalyActionEvent(10.0, (state) => segments[2].MotorState.An_LoadJam.True() ),
    new ToggleAnomalyActionEvent(15.0, (state) => supplyState.An_UnderVoltage.True() ),
    new ToggleAnomalyActionEvent(16.5, (state) => supplyState.An_UnderVoltage.False() ),
];

// ----------------------------
// Start OPC UA server (exposes Supply + 5 segments + packages)
// ----------------------------
ConveyorSimApp.OpcUa.MyNodeManager ns = default;
var opc = await MyOpcUaServerHost.StartAsync("ConveyorSim", "opc.tcp://localhost:4840/ConveyorSim", createNodeManager: (Func<Opc.Ua.Server.IServerInternal, ApplicationConfiguration, Opc.Ua.Server.CustomNodeManager2>)((srv, conf) => {

    ns = new MyNodeManager(srv, conf, "Conveyor", "urn:ConveyorSim:NodeManager", BuildConveyorNamespace);
    ns.SystemContext.NodeIdFactory = ns;
    return (Opc.Ua.Server.CustomNodeManager2)ns;

}));

// ----------------------------
// Simulation timing controls
// ----------------------------
// Defaults: 10 minutes sim, half-speed (slower), 2s start delay
double totalTimeSec = 600.0;
double dt = 0.01;
double samplePeriod = 0.5;
double speedFactor = 0.5;     // 1.0 = real-time, 0.5 = half-speed (slower), 2.0 = 2x faster
double startDelaySec = 2.0;   // time to attach OPC UA client

// Override via args: [0]=duration, [1]=speedFactor, [2]=startDelay
if (args.Length > 0 && double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var argDur)) totalTimeSec = argDur;
if (args.Length > 1 && double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var argSpeed)) speedFactor = Math.Max(1e-3, argSpeed);
if (args.Length > 2 && double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var argDelay)) startDelaySec = Math.Max(0.0, argDelay);

// Or environment
if (double.TryParse(Environment.GetEnvironmentVariable("SIM_DURATION_SEC"), NumberStyles.Float, CultureInfo.InvariantCulture, out var envDur)) totalTimeSec = envDur;
if (double.TryParse(Environment.GetEnvironmentVariable("SIM_SPEED_FACTOR"), NumberStyles.Float, CultureInfo.InvariantCulture, out var envSpeed)) speedFactor = Math.Max(1e-3, envSpeed);
if (double.TryParse(Environment.GetEnvironmentVariable("SIM_START_DELAY_SEC"), NumberStyles.Float, CultureInfo.InvariantCulture, out var envDelay)) startDelaySec = Math.Max(0.0, envDelay);

Console.WriteLine($"Starting simulation for {totalTimeSec:F0}s sim-time at speedFactor={speedFactor:F2} (1.0=real-time). Start delay={startDelaySec:F1}s.");
if (startDelaySec > 0) await Task.Delay(TimeSpan.FromSeconds(startDelaySec));

// Wall-clock throttle to achieve speedFactor (sim seconds per real second)
var sw = Stopwatch.StartNew();
var nextWall = sw.Elapsed;

// ----------------------------
// Run loop
// ----------------------------
double nextSample = 0.0;
int idx = 0;

var simState = new SimState((string key, bool enable) => { /* not used here */ });

PrintHeader();
while (simState.Time < totalTimeSec)
{
    // Fire due scenario events
    while (idx < scenario.Length && scenario[idx].Time <= simState.Time + 1e-9)
    {
        scenario[idx].Apply(simState);
        idx++;
    }

    // Spawn packages
    if (simState.Time >= nextSpawn)
    {
        var package = new Package();
        package.PositionM.Reset();
        package.PositionM.Set(0.5 + Random.Shared.NextDouble() * (20.0 - 0.5));
        packages.Add(package);
        nextSpawn += packageSpawnPeriod;
    }

    // Step time
    simState.Step(dt);

    // 0) Update grid
    supply.Step(dt, simState);

    // Update OPC UA Supply
    UpdateBindables(supplyState, supplyOutputs);

    // For each segment
    for (int i = 0; i < Segments; i++)
    {
        var seg = segments[i];

        // 1) Feed supply to VFD inputs
        seg.VfdInputs.SupplyVoltageLL.Set(supplyOutputs.LineLineVoltage);
        seg.VfdInputs.SupplyFrequency.Set(supplyOutputs.Frequency);

        // 2) VFD step
        seg.Vfd.Step(dt, simState);

        // 3) Wire VFD outputs -> motor inputs
        seg.MotorInputs.DriveFrequencyCmd.Set(seg.VfdOutputs.OutputFrequency);
        seg.MotorInputs.DriveVoltageCmd.Set(seg.VfdOutputs.OutputVoltage);

        // 4) Compute belt speed and dynamic load torque from packages on this segment
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

        // Resistive + inertial torque reflected to motor shaft
        double F = segmentMass * 9.81 * MuRoll + segmentMass * dv; // N
        double T_load_pkg = (F * PulleyRadiusM) / (GearRatio * MechEfficiency); // Nm

        // Update motor base load torque dynamically
        seg.MotorSettings.ConstLoadTorque = seg.BaseConstTorque + T_load_pkg;

        // 5) Motor step
        seg.Motor.Step(dt, simState);

        // 6) Wire motor current back to VFD
        seg.VfdInputs.MotorCurrentFeedback.Set(seg.MotorOutputs.PhaseCurrent);

        // 7) VFD thermal & trips
        seg.Vfd.Step2(dt, simState);

        // Update OPC UA Segment
        UpdateBindables(seg.VfdState, seg.VfdInputs, seg.VfdOutputs, seg.MotorState, seg.MotorInputs, seg.MotorOutputs);
    }

    // Move packages along belt
    for (int p = packages.Count - 1; p >= 0; p--)
    {
        var pkg = packages[p];
        int segIdx = Math.Clamp((int)Math.Floor(pkg.PositionM / SegmentLengthM), 0, Segments - 1);
        double beltSpeed = BeltSpeedFromRpm(segments[segIdx].MotorState.SpeedRpm);
        pkg.PositionM.Add(beltSpeed * dt);

        if (pkg.PositionM >= ConveyorLengthM)
            packages.RemoveAt(p);
    }

    // Update OPC UA packages
    UpdatePackages(packages);

    // Sampled print
    if (simState.Time >= nextSample)
    {
        PrintStatus(simState.Time, segments, packages);
        nextSample += samplePeriod;
    }

    // Throttle to desired wall-clock speed
    nextWall += TimeSpan.FromSeconds(dt / Math.Max(1e-6, speedFactor));
    var delay = nextWall - sw.Elapsed;
    if (delay > TimeSpan.Zero)
        await Task.Delay(delay);
}

// Event log
Console.WriteLine();
Console.WriteLine("Event log:");
foreach (var e in simState.EventLog) Console.WriteLine(" - " + e);

// Stop OPC UA server
await opc.StopAsync();

// ----------------------------
// Helpers & types
// ----------------------------
double BeltSpeedFromRpm(double rpm) => (2.0 * Math.PI * rpm / 60.0) * (PulleyRadiusM / GearRatio); // m/s

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
                var db = (DoubleBindable)prop.GetValue(value);
                ns.UpdateDoubleBindable(db);
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
    //Pkg_Count.Value = pkgs.Count;
    //Pkg_Positions.Value = positions;
    //Pkg_Masses.Value = masses;

    //Pkg_Count.ClearChangeMasks(Ctx, false);
    //Pkg_Positions.ClearChangeMasks(Ctx, false);
    //Pkg_Masses.ClearChangeMasks(Ctx, false);
}

void BuildConveyorNamespace(NodeState rootNode)
{
    // Supply
    var supply = rootNode.AddFolder("Supply");
    supply.AddVar(supplyOutputs, xx => xx.LineLineVoltage);
    supply.AddVar(supplyOutputs, xx => xx.Frequency);
    supply.AddVar(supplyState, xx => xx.TargetVoltageLL);
    supply.AddVar(supplyState, xx => xx.TargetFrequency);
    supply.AddVar(supplyState, xx => xx.An_UnderVoltage);
    supply.AddVar(supplyState, xx => xx.An_OverVoltage);
    supply.AddVar(supplyState, xx => xx.An_FrequencyDrift);

    // Segments
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

    // Packages
    var pkgs = rootNode.AddFolder("Packages");
    pkgs.AddVar<int>("Count", DataTypeIds.Int32);
    pkgs.AddArrayVar<double>("Positions");
    pkgs.AddArrayVar<double>("Masses");
}
