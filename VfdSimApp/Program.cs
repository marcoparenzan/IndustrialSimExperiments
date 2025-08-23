using InductionMotorSimLib;
using IndustrialSimLib;
using IndustrialSimLib.SimEvents;
using System.Globalization;
using VFDSimLib;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var vfdSettings = new VfdSettings
{
    RatedVoltageLL = 400.0, // VAC line-line
    RatedFrequency = 50.0,  // Hz
    MaxCurrent = 30.0,      // A (trip threshold uses a multiple)
    Accel = 10.0,           // Hz/s
    Decel = 10.0,           // Hz/s
    VoltBoost = 0.07,       // p.u. of rated voltage at low speed
    ThermalTimeConstant = 40.0, // s (heatsink)
    MaxHeatsinkTemp = 85.0, // °C trip
    AmbientTemp = 25.0,     // °C
    OverCurrentMultiple = 1.6, // I_trip = multiple * MaxCurrent
    UnderVoltPUNomDC = 0.55,   // trip if Vdc < 0.55 * Vdc_nom
    OverVoltPUNomDC = 1.20   // trip if Vdc > 1.20 * Vdc_nom
};
var vdcNorm = Math.Sqrt(2.0) * vfdSettings.RatedVoltageLL;
var vfdState = new VfdState
{
    VdcNom = vdcNorm,
    BusVoltage = vdcNorm,
    HeatsinkTemp = vfdSettings.AmbientTemp,
    TargetFrequency = 50,
    OutputFrequency = 0
};
// ~565V for 400Vac

var motorSettings = new InductionMotorSettings
{
    VratedLL = 400.0, // VAC
    Frated = 50.0,    // Hz
    PolePairs = 2,       // 4-pole machine

    // Rated / base values for a ~7.5 kW motor (approx)
    RatedPower = 7500.0,  // W
    RatedSpeedRpm = 1440.0, // rpm
    Inertia = 0.20,       // kg*m^2 (motor + load reflected)
    ViscFriction = 0.003, // Nm per rpm
    CoulombFriction = 1.0, // Nm
    SlipNom = 0.03,       // nominal slip at rated
    TorqueMaxPU = 2.2,    // multiple of rated torque
    Inom = 15.0,          // A (phase rms)

    // Load model
    ConstLoadTorque = 5.0, // Nm base constant load
    JamExtraTorque = 80.0, // Nm extra when jammed
    BearingExtraTorque = 5.0, // Nm extra when bearings worn
};
var omegaRated = 2.0 * Math.PI * (motorSettings.RatedSpeedRpm / 60.0);
var motorState = new InductionMotorState 
{
    OutputFrequency = vfdState.OutputFrequency, 
    OutputVoltage = vfdState.OutputVoltage, 
    SpeedRpm = 0 ,
    VratedPhPh = motorSettings.VratedLL,
    // Rated torque: P = T * omega  =>  T = P / (2*pi*n/60)
    Trated = motorSettings.RatedPower / omegaRated,
};

var motor = new InductionMotor(motorSettings, motorState);
var vfd = new Vfd(vfdSettings, vfdState);

SimEvent[] scenario = [

    // Inject a load jam at 5s for 7s
    new ToggleAnomalyEvent{ Time = 5.0, Key = "loadjam", Enable = true },
    new ToggleAnomalyEvent{ Time = 12.0, Key = "loadjam", Enable = false },

    // Under-voltage sag at 15s
    new ToggleAnomalyEvent{ Time = 15.0, Key = "undervoltage", Enable = true },
    new ToggleAnomalyEvent{ Time = 16.5, Key = "undervoltage", Enable = false },

    // Phase loss at 20s (likely trips soon)
    new ToggleAnomalyEvent{ Time = 20.0, Key = "phaseloss", Enable = true },

    // Attempt auto reset at 22.0s so we can continue
    new ResetTripEvent{ Time = 22.0 },
    new ToggleAnomalyEvent{ Time = 22.0, Key = "phaseloss", Enable = false },

    // Bearing wear after 24.0s (persistent)
    new ToggleAnomalyEvent{ Time = 24.0, Key = "bearingwear", Enable = true },
];

// --- Run ---
double totalTimeSec = 30.0; // s
double dt = 0.01;        // s integration step
double samplePeriod = 0.5;     // s print period

var simState = new SimState((string key, bool enable)=>
{
    switch (key.ToLowerInvariant())
    {
        case "undervoltage": vfdState.An_UnderVoltage = enable; break;
        case "overvoltage": vfdState.An_OverVoltage = enable; break;
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
    // Fire due events
    while (idx < scenario.Length && scenario[idx].Time <= simState.Time + 1e-9)
    {
        scenario[idx].Apply(simState);
        idx++;
    }

    simState.Step(dt);

    // 1) Update VFD
    vfd.Step(dt, simState);

    // 2) Propagate VFD outputs to motor inputs (missing link)
    motorState.OutputFrequency = vfdState.OutputFrequency;
    motorState.OutputVoltage = vfdState.OutputVoltage;

    // 3) Update motor using the new VFD outputs
    motor.Step(dt, simState);

    // 4) Feed motor current back to the VFD for losses/trips
    vfdState.MotorCurrent = motorState.PhaseCurrent;

    // 5) Run VFD thermal and trip logic
    vfd.Step2(dt, simState);

    if (simState.Time >= nextSample)
    {
        Console.WriteLine(
            $"{simState.Time,6:F2}  {vfdState.OutputFrequency,5:F1}  {vfdState.OutputVoltage,5:F0}  {vfdState.MotorCurrent,5:F1}  {motorState.SpeedRpm,6:F0}  {motorState.ElectTorque,6:F1}  {vfdState.HeatsinkTemp,6:F1}  {vfdState.BusVoltage,4:F0}  {(simState.Running ? "Y" : "N")}   {simState.ActiveTrip.Code}");
        nextSample += samplePeriod;
    }
}

// Print event log at the end
Console.WriteLine();
Console.WriteLine("Event log:");
foreach (var e in simState.EventLog)
    Console.WriteLine(" - " + e);
