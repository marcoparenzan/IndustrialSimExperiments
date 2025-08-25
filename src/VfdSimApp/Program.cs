using InductionMotorSimLib;
using IndustrialSimLib;
using IndustrialSimLib.SimEvents;
using System.Globalization;
using ThreePhaseSupplySimLib;
using VfdSimLib;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// VFD settings/state as before...
var vfdSettings = new VfdSettings
{
    RatedVoltageLL = 400.0,
    RatedFrequency = 50.0,
    MaxCurrent = 30.0,
    Accel = 10.0,
    Decel = 10.0,
    VoltBoost = 0.07,
    ThermalTimeConstant = 40.0,
    MaxHeatsinkTemp = 85.0,
    AmbientTemp = 25.0,
    OverCurrentMultiple = 1.6,
    UnderVoltPUNomDC = 0.55,
    OverVoltPUNomDC = 1.20
};
var vdcNorm = Math.Sqrt(2.0) * vfdSettings.RatedVoltageLL;
var vfdState = new VfdState
{
    VdcNom = vdcNorm,
    BusVoltage = vdcNorm,
    HeatsinkTemp = vfdSettings.AmbientTemp,
    TargetFrequency = 50
};

// Supply settings/state/io
var supplySettings = new ThreePhaseSupplySettings
{
    NominalVoltageLL = vfdSettings.RatedVoltageLL,
    NominalFrequency = vfdSettings.RatedFrequency,
    VoltageSlewRate = 1000.0,
    FrequencySlewRate = 10.0,
    UnderVoltPU = 0.50, // match old behavior
    OverVoltPU = 1.25   // match old behavior
};
var supplyState = new ThreePhaseSupplyState
{
    TargetVoltageLL = supplySettings.NominalVoltageLL,
    TargetFrequency = supplySettings.NominalFrequency
};
var supplyInputs = new ThreePhaseSupplyInputs();
var supplyOutputs = new ThreePhaseSupplyOutputs { LineLineVoltage = supplySettings.NominalVoltageLL, Frequency = supplySettings.NominalFrequency };

// Motor settings/state as before...
var motorSettings = new InductionMotorSettings
{
    RatedVoltageLL = 400.0,
    RatedFrequency = 50.0,
    PolePairs = 2,
    RatedPower = 7500.0,
    RatedSpeedRpm = 1440.0,
    Inertia = 0.20,
    ViscFriction = 0.003,
    CoulombFriction = 1.0,
    SlipNom = 0.03,
    TorqueMaxPU = 2.2,
    Inom = 15.0,
    ConstLoadTorque = 5.0,
    JamExtraTorque = 80.0,
    BearingExtraTorque = 5.0
};
var omegaRated = 2.0 * Math.PI * (motorSettings.RatedSpeedRpm / 60.0);
var motorState = new InductionMotorState
{
    SpeedRpm = 0,
    VratedPhPh = motorSettings.RatedVoltageLL,
    Trated = motorSettings.RatedPower / omegaRated,
};

// Wiring ports
var vfdInputs = new VfdInputs();
var vfdOutputs = new VfdOutputs();
var motorInputs = new InductionMotorInputs();
var motorOutputs = new InductionMotorOutputs();

// Devices
var supply = new ThreePhaseSupply(supplySettings, supplyState, supplyInputs, supplyOutputs);
var motor = new InductionMotor(motorSettings, motorState, motorInputs, motorOutputs);
var vfd = new Vfd(vfdSettings, vfdState, vfdInputs, vfdOutputs);

SimEvent[] scenario = [
    new ToggleAnomalyEvent{ Time = 5.0, Key = "loadjam", Enable = true },
    new ToggleAnomalyEvent{ Time = 12.0, Key = "loadjam", Enable = false },
    new ToggleAnomalyEvent{ Time = 15.0, Key = "undervoltage", Enable = true },
    new ToggleAnomalyEvent{ Time = 16.5, Key = "undervoltage", Enable = false },
    new ToggleAnomalyEvent{ Time = 20.0, Key = "phaseloss", Enable = true },
    new ResetTripEvent{ Time = 22.0 },
    new ToggleAnomalyEvent{ Time = 22.0, Key = "phaseloss", Enable = false },
    new ToggleAnomalyEvent{ Time = 24.0, Key = "bearingwear", Enable = true },
];

// --- Run ---
double totalTimeSec = 30.0;
double dt = 0.01;
double samplePeriod = 0.5;

var simState = new SimState((string key, bool enable)=>
{
    switch (key.ToLowerInvariant())
    {
        case "undervoltage": supplyState.An_UnderVoltage = enable; break;
        case "overvoltage": supplyState.An_OverVoltage = enable; break;
        case "phaseloss": vfdState.An_PhaseLoss = enable; motorState.An_PhaseLoss = enable; break;
        case "groundfault": vfdState.An_GroundFault = enable; break;
        case "loadjam": motorState.An_LoadJam = enable; break;
        case "bearingwear": motorState.An_BearingWear = enable; break;
        case "sensornoise": motorState.An_SensorNoise = enable; break;
        default: break;
    }
});

double nextSample = 0.0;
int idx = 0;

Console.WriteLine("time    f_out  V_out  I(A)   rpm     T(Nm)  T_hs(°C) Vdc   RUN Trip");
Console.WriteLine(new string('-', 78));

while (simState.Time < totalTimeSec)
{
    while (idx < scenario.Length && scenario[idx].Time <= simState.Time + 1e-9)
    {
        scenario[idx].Apply(simState);
        idx++;
    }

    simState.Step(dt);

    // 0) Update supply (grid)
    supply.Step(dt, simState);

    // 1) Update VFD (feed supply into VFD inputs)
    vfdInputs.SupplyVoltageLL = supplyOutputs.LineLineVoltage;
    vfdInputs.SupplyFrequency = supplyOutputs.Frequency;
    vfd.Step(dt, simState);

    // 2) Wire VFD outputs to motor inputs
    motorInputs.DriveFrequencyCmd = vfdOutputs.OutputFrequency;
    motorInputs.DriveVoltageCmd = vfdOutputs.OutputVoltage;

    // 3) Update motor
    motor.Step(dt, simState);

    // 4) Wire motor current back to the VFD inputs
    vfdInputs.MotorCurrentFeedback = motorOutputs.PhaseCurrent;

    // 5) Run VFD thermal and trip logic
    vfd.Step2(dt, simState);

    if (simState.Time >= nextSample)
    {
        Console.WriteLine(
            $"{simState.Time,6:F2}  {vfdOutputs.OutputFrequency,5:F1}  {vfdOutputs.OutputVoltage,5:F0}  {motorOutputs.PhaseCurrent,5:F1}  {motorState.SpeedRpm,6:F0}  {motorState.ElectTorque,6:F1}  {vfdState.HeatsinkTemp,6:F1}  {vfdState.BusVoltage,4:F0}  {(simState.Running ? "Y" : "N")}   {simState.ActiveTrip.Code}");
        nextSample += samplePeriod;
    }
}

Console.WriteLine();
Console.WriteLine("Event log:");
foreach (var e in simState.EventLog)
    Console.WriteLine(" - " + e);
