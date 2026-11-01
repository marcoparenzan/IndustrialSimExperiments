namespace VfdSimLib;

/// <summary>Where the drive takes its run command and speed reference from.</summary>
public enum VfdControlSource
{
    /// <summary>The target frequency is set directly through <see cref="VfdState.TargetFrequency"/> (default, keypad-like).</summary>
    Setpoint,
    /// <summary>Control terminals: run forward, multi-step speed selection, analog speed reference and fault reset.</summary>
    Terminals,
}

/// <summary>Signal type of the analog speed reference terminal (AVI).</summary>
public enum VfdAnalogInputMode
{
    Voltage0To10,   // volts, 0 V = 0 Hz, 10 V = MaxOutputFrequency
    Current4To20,   // milliamps, 4 mA = 0 Hz, 20 mA = MaxOutputFrequency; below 4 mA is clamped to 0 Hz
}
