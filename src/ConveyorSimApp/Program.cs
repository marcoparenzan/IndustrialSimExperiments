using InductionMotorSimLib;
using IndustrialSimLib;
using IndustrialSimLib.SimEvents;
using PackageSimLib;
using System.Globalization;
using ThreePhaseSupplySimLib;
using VfdSimLib;
using ConveyorSimApp.OpcUa; // + add this using
using System.Diagnostics;
using OpcUaServerLib;   // throttle timing

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// ----------------------------
// Conveyor configuration
// ----------------------------
const int Segments = 5;
const double ConveyorLengthM = 50.0;
const double SegmentLengthM = ConveyorLengthM / Segments;

// Mechanics per segment (motor -> gearbox -> pulley)
// motor RPM -> belt speed: v = (2π * rpm / 60) * (PulleyRadius / GearRatio)
const double PulleyRadiusM = 0.15; // 300 mm diameter drum
const double GearRatio = 12.0;     // motor:drum speed ratio
const double MechEfficiency = 0.9; // mechanical efficiency motor→belt
const double MuRoll = 0.03;        // rolling resistance coeff

// Package generation
var rand = new Random(123);
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
var supplyState = new ThreePhaseSupplyState
{
    TargetVoltageLL = supplySettings.NominalVoltageLL,
    TargetFrequency = supplySettings.NominalFrequency
};
var supplyInputs = new ThreePhaseSupplyInputs();
var supplyOutputs = new ThreePhaseSupplyOutputs
{
    LineLineVoltage = supplySettings.NominalVoltageLL,
    Frequency = supplySettings.NominalFrequency
};
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
for (int i = 0; i < Segments; i++)
{
    var vfdState = new VfdState
    {
        VdcNom = Math.Sqrt(2.0) * vfdSettings.RatedVoltageLL,
        BusVoltage = Math.Sqrt(2.0) * vfdSettings.RatedVoltageLL,
        HeatsinkTemp = vfdSettings.AmbientTemp,
        TargetFrequency = vfdTargetFreqHz
    };
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
    var motorState = new InductionMotorState
    {
        SpeedRpm = 0,
        VratedPhPh = motorSettings.RatedVoltageLL,
        Trated = motorSettings.RatedPower / omegaRated
    };
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
    new ToggleAnomalyActionEvent( 5.0, (state) => segments[2].MotorState.An_LoadJam = true ),
    new ToggleAnomalyActionEvent(10.0, (state) => segments[2].MotorState.An_LoadJam = false ),
    new ToggleAnomalyActionEvent(15.0, (state) => supplyState.An_UnderVoltage = true ),
    new ToggleAnomalyActionEvent(16.5, (state) => supplyState.An_UnderVoltage = false ),
];

// ----------------------------
// Start OPC UA server (exposes Supply + 5 segments + packages)
// ----------------------------
var opcBindings = new ConveyorNodeBindings();
var opc = await MyOpcUaServerHost.StartAsync("ConveyorSim", "opc.tcp://localhost:4840/ConveyorSim", createNodeManager: (srv, conf) => {

    var ns = new ConveyorNodeManager(srv, conf, opcBindings, supplyState, supplyOutputs, segments, packages.ToArray());
    return ns;

});

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
        packages.Add(new Package
        {
            PositionM = 0.0,
            MassKg = 0.5 + rand.NextDouble() * (20.0 - 0.5)
        });
        nextSpawn += packageSpawnPeriod;
    }

    // Step time
    simState.Step(dt);

    // 0) Update grid
    supply.Step(dt, simState);

    // Update OPC UA Supply
    opcBindings.UpdateSupply(supplyState, supplyOutputs);

    // For each segment
    for (int i = 0; i < Segments; i++)
    {
        var seg = segments[i];

        // 1) Feed supply to VFD inputs
        seg.VfdInputs.SupplyVoltageLL = supplyOutputs.LineLineVoltage;
        seg.VfdInputs.SupplyFrequency = supplyOutputs.Frequency;

        // 2) VFD step
        seg.Vfd.Step(dt, simState);

        // 3) Wire VFD outputs -> motor inputs
        seg.MotorInputs.DriveFrequencyCmd = seg.VfdOutputs.OutputFrequency;
        seg.MotorInputs.DriveVoltageCmd = seg.VfdOutputs.OutputVoltage;

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
        seg.VfdInputs.MotorCurrentFeedback = seg.MotorOutputs.PhaseCurrent;

        // 7) VFD thermal & trips
        seg.Vfd.Step2(dt, simState);

        // Update OPC UA Segment
        opcBindings.UpdateSegment(i, seg.VfdState, seg.VfdInputs, seg.VfdOutputs,
                                      seg.MotorState, seg.MotorInputs, seg.MotorOutputs);
    }

    // Move packages along belt
    for (int p = packages.Count - 1; p >= 0; p--)
    {
        var pkg = packages[p];
        int segIdx = Math.Clamp((int)Math.Floor(pkg.PositionM / SegmentLengthM), 0, Segments - 1);
        double beltSpeed = BeltSpeedFromRpm(segments[segIdx].MotorState.SpeedRpm);
        pkg.PositionM += beltSpeed * dt;

        if (pkg.PositionM >= ConveyorLengthM)
            packages.RemoveAt(p);
    }

    // Update OPC UA packages
    opcBindings.UpdatePackages(packages);

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
static double BeltSpeedFromRpm(double rpm) =>
    (2.0 * Math.PI * rpm / 60.0) * (PulleyRadiusM / GearRatio); // m/s

static void PrintHeader()
{
    Console.WriteLine("time    seg  f_out(Hz)  V_out(V)   I(A)   rpm    v_belt(m/s)  pkgs  T_hs(°C)  Vdc(V)  RUN Trip");
    Console.WriteLine(new string('-', 110));
}

static void PrintStatus(double t, Segment[] segs, List<Package> pkgs)
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

sealed class Segment
{
    public int Index { get; set; }
    public double StartM { get; set; }
    public double EndM { get; set; }

    public Vfd Vfd { get; set; }
    public VfdState VfdState { get; set; }
    public VfdInputs VfdInputs { get; set; }
    public VfdOutputs VfdOutputs { get; set; }

    public InductionMotor Motor { get; set; }
    public InductionMotorState MotorState { get; set; }
    public InductionMotorSettings MotorSettings { get; set; }
    public InductionMotorInputs MotorInputs { get; set; }
    public InductionMotorOutputs MotorOutputs { get; set; }

    public double BaseConstTorque { get; set; }
    public double LastBeltSpeed { get; set; }

    public bool Running { get; set; } = true; // In this model, simState.Running is global; segments can still log trips
    public string Trip => FaultToString();

    private string FaultToString()
    {
        // Here we don’t keep per-segment fault code; rely on event log/global if needed.
        // Extend this if you add per-segment fault tracking.
        return "";
    }
}