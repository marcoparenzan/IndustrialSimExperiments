using IndustrialSimLib;

namespace VfdSimLib;

public class VfdInputs
{
    // Feedback from motor to the VFD
    public DoubleBindable MotorCurrentFeedback { get; } = new();  // A (rms)
    // Control terminals (DI1.. and AVI), used when VfdSettings.ControlSource = Terminals
    public BoolBindable RunForward { get; } = new();      // run command; the drive runs forward only
    public BoolBindable FaultReset { get; } = new();      // rising edge clears an active trip
    public BoolBindable MultiStep1 { get; } = new();
    public BoolBindable MultiStep2 { get; } = new();
    public BoolBindable MultiStep3 { get; } = new();
    public DoubleBindable AnalogSpeedRef { get; } = new(); // V or mA, see VfdSettings.AnalogMode
    public DoubleBindable MotorPowerFeedback { get; } = new();    // W, motor shaft power; negative = regenerating into the DC link

    // Grid/supply inputs
    public DoubleBindable SupplyVoltageLL { get; } = new();   // V (rms L-L)
    public DoubleBindable SupplyFrequency { get; } = new();  // Hz
}

public class VfdOutputs
{
    // VFD outputs toward the motor
    public DoubleBindable OutputFrequency { get; } = new();   // Hz
    public DoubleBindable OutputVoltage { get; } = new();
    // Output terminals
    public BoolBindable FaultRelay { get; } = new();             // relay RA/RC: closed while a trip is active
    public BoolBindable RunningRelay { get; } = new();           // closed while the drive outputs a frequency
    public DoubleBindable AnalogOutFrequency { get; } = new();   // AFM, 0..10 V proportional to OutputFrequency / MaxOutputFrequency     // V (rms, LL approx)
}