using InductionMotorSimLib;
using IndustrialSimLib;
using VfdSimLib;

namespace ConveyorSimApp.Models;

public class Segment
{
    public int Index { get; set; }
    public DoubleBindable StartM { get; set; }
    public DoubleBindable EndM { get; set; }

    public Vfd Vfd { get; set; }
    public VfdState VfdState { get; set; }
    public VfdInputs VfdInputs { get; set; }
    public VfdOutputs VfdOutputs { get; set; }

    public InductionMotor Motor { get; set; }
    public InductionMotorState MotorState { get; set; }
    public InductionMotorSettings MotorSettings { get; set; }
    public InductionMotorInputs MotorInputs { get; set; }
    public InductionMotorOutputs MotorOutputs { get; set; }

    public DoubleBindable BaseConstTorque { get; set; }
    public DoubleBindable LastBeltSpeed { get; set; }

    public bool Running { get; set; } = true; // In this model, simState.Running is global; segments can still log trips
    public string Trip => FaultToString();

    private string FaultToString()
    {
        // Here we don’t keep per-segment fault code; rely on event log/global if needed.
        // Extend this if you add per-segment fault tracking.
        return "";
    }
}