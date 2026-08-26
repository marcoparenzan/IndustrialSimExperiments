using InductionMotorSimLib;
using VfdSimLib;

namespace ConveyorSimLib;

internal sealed record ConveyorSegment(
    int Index,
    double StartMeters,
    double EndMeters,
    Vfd Vfd,
    VfdState VfdState,
    VfdInputs VfdInputs,
    VfdOutputs VfdOutputs,
    InductionMotor Motor,
    InductionMotorState MotorState,
    InductionMotorSettings MotorSettings,
    InductionMotorInputs MotorInputs,
    InductionMotorOutputs MotorOutputs,
    double BaseLoadTorque)
{
    public double LastBeltSpeed { get; set; }
}
