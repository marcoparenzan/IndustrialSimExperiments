using System;
using System.Collections.Generic;
using System.Globalization;

// ------------------------------------------------------------
// VFD + Induction Motor (simplified) simulator with anomalies
// Single-file C# console app (targets .NET 6+). 
// - V/f control with accel/decel ramps
// - Simplified induction motor torque model
// - Thermal model for inverter heatsink
// - Fault detection (UV/OV/OC/OT, phase loss, ground fault)
// - Anomaly injection via timed scenario events
// - Text table output every samplePeriod seconds
// ------------------------------------------------------------

namespace VfdSim
{
    #region Config & State

    public enum FaultCode
    {
        None,
        UnderVoltage,
        OverVoltage,
        OverCurrent,
        OverTemp,
        GroundFault,
        PhaseLoss
    }

    public class VfdSettings
    {
        public double RatedVoltageLL = 400.0; // VAC line-line
        public double RatedFrequency = 50.0;  // Hz
        public double MaxCurrent = 30.0;      // A (trip threshold uses a multiple)
        public double Accel = 10.0;           // Hz/s
        public double Decel = 10.0;           // Hz/s
        public double VoltBoost = 0.07;       // p.u. of rated voltage at low speed
        public double ThermalTimeConstant = 40.0; // s (heatsink)
        public double MaxHeatsinkTemp = 85.0; // °C trip
        public double AmbientTemp = 25.0;     // °C
        public double OverCurrentMultiple = 1.6; // I_trip = multiple * MaxCurrent
        public double UnderVoltPUNomDC = 0.55;   // trip if Vdc < 0.55 * Vdc_nom
        public double OverVoltPUNomDC = 1.20;   // trip if Vdc > 1.20 * Vdc_nom
    }

    public class VfdState
    {
        public double Time;                  // s
        public bool Running = true;
        public double TargetFrequency;       // Hz
        public double OutputFrequency;       // Hz
        public double OutputVoltage;         // V (fundamental rms, phase-phase approx)
        public double BusVoltage;            // Vdc
        public double MotorCurrent;          // A (rms, estimate)
        public double HeatsinkTemp;          // °C
        public FaultCode ActiveTrip = FaultCode.None;
        public readonly List<string> EventLog = new();

        // Anomaly toggles
        public bool An_UnderVoltage;
        public bool An_OverVoltage;
        public bool An_PhaseLoss;
        public bool An_GroundFault;

        public void Trip(FaultCode fc)
        {
            if (ActiveTrip != FaultCode.None) return;
            ActiveTrip = fc;
            Running = false;
            EventLog.Add($"[{Time,6:F2}s] TRIP: {fc}");
        }

        public void ResetTrip()
        {
            ActiveTrip = FaultCode.None;
            Running = true;
            EventLog.Add($"[{Time,6:F2}s] RESET trip");
        }
    }

    public class MotorParams
    {
        public double VratedLL = 400.0; // VAC
        public double Frated = 50.0;    // Hz
        public int PolePairs = 2;       // 4-pole machine

        // Rated / base values for a ~7.5 kW motor (approx)
        public double RatedPower = 7500.0;  // W
        public double RatedSpeedRpm = 1440.0; // rpm
        public double Inertia = 0.20;       // kg*m^2 (motor + load reflected)
        public double ViscFriction = 0.003; // Nm per rpm
        public double CoulombFriction = 1.0; // Nm
        public double SlipNom = 0.03;       // nominal slip at rated
        public double TorqueMaxPU = 2.2;    // multiple of rated torque
        public double Inom = 15.0;          // A (phase rms)

        // Load model
        public double ConstLoadTorque = 5.0; // Nm base constant load
        public double JamExtraTorque = 80.0; // Nm extra when jammed
        public double BearingExtraTorque = 5.0; // Nm extra when bearings worn
    }

    public class MotorState
    {
        public double SpeedRpm;    // rpm
        public double ElectTorque; // Nm
        public double PhaseCurrent;// A

        // Anomaly toggles
        public bool An_LoadJam;
        public bool An_BearingWear;
        public bool An_SensorNoise;
    }

    #endregion

    #region Motor Model

    public class InductionMotor
    {
        private readonly MotorParams p;
        private readonly MotorState s;

        private readonly double Trated; // Nm
        private readonly double VratedPhPh;
        private readonly Random rng = new(1234);

        public InductionMotor(MotorParams p, MotorState s)
        {
            this.p = p; this.s = s;
            VratedPhPh = p.VratedLL;
            // Rated torque: P = T * omega  =>  T = P / (2*pi*n/60)
            double omegaRated = 2.0 * Math.PI * (p.RatedSpeedRpm / 60.0);
            Trated = p.RatedPower / omegaRated;
        }

        public (double TorqueNm, double PhaseCurrentA, double Slip, double SyncRpm) Step(
            double dt, double VoutLL, double foutHz, double busVdc)
        {
            // Handle zero/near-zero frequency (hold torque limited)
            double f = Math.Max(0.1, Math.Abs(foutHz));
            double nSyncRpm = 60.0 * f / p.PolePairs; // synchronous speed

            // Slip (simple definition)
            double slip = nSyncRpm <= 1e-3 ? 1.0 : Math.Max(0.0, (nSyncRpm - s.SpeedRpm) / nSyncRpm);

            // V/f ratio + low-speed boost influence torque
            double vf_pu = (VoutLL / Math.Max(10.0, VratedPhPh)) / (f / Math.Max(1.0, p.Frated));
            vf_pu = Math.Clamp(vf_pu, 0.0, 1.2);

            // Simplified torque model: T ~ Trated * (vf)^2 * (slip / (slip + s0)) * torque_limit
            double torquePU = Math.Pow(vf_pu, 2.0) * (slip / (slip + p.SlipNom));
            double T_e = torquePU * Trated;
            T_e = Math.Clamp(T_e, -p.TorqueMaxPU * Trated, p.TorqueMaxPU * Trated);

            // Load torque: constant + viscous + Coulomb + anomalies
            double T_load = p.ConstLoadTorque + p.ViscFriction * Math.Abs(s.SpeedRpm) + p.CoulombFriction * Math.Sign(s.SpeedRpm);
            if (s.An_LoadJam) T_load += p.JamExtraTorque;
            if (s.An_BearingWear) T_load += p.BearingExtraTorque;

            // Net acceleration: J * dω = (T_e - T_load)
            double domega = (T_e - T_load) / Math.Max(1e-6, p.Inertia); // rad/s^2
            double domega_rpm = domega * 60.0 / (2.0 * Math.PI);

            s.SpeedRpm += domega_rpm * dt;
            // Prevent running backwards in this simple model
            if (s.SpeedRpm < 0 && foutHz >= 0) s.SpeedRpm = 0;

            // Current proxy: proportional to torque demand divided by (V/f) margin
            double vf_margin = Math.Max(0.2, vf_pu);
            double I = Math.Abs(T_e) / Math.Max(1e-3, Trated) * (1.0 / vf_margin) * p.Inom;

            // Phase loss anomaly => higher current for same torque, lower torque capability
            if (Program.Vfd?.State.An_PhaseLoss == true)
            {
                I *= 1.7;         // current inflates
                T_e *= 0.6;       // torque drops
            }

            // Sensor noise affects only reported speed (if someone reads it)
            double noise = s.An_SensorNoise ? (rng.NextDouble() - 0.5) * 8.0 : 0.0; // +/- 4 rpm noise

            s.ElectTorque = T_e;
            s.PhaseCurrent = I;

            return (T_e, I, slip, nSyncRpm + noise);
        }

        public MotorState State => s;
    }

    #endregion

    #region VFD Model

    public class Vfd
    {
        public readonly VfdSettings Cfg;
        public readonly VfdState State;
        private readonly InductionMotor motor;

        private readonly double VdcNom; // ~ sqrt(2) * VLL

        public Vfd(VfdSettings cfg, VfdState st, InductionMotor motor)
        {
            Cfg = cfg; State = st; this.motor = motor;
            VdcNom = Math.Sqrt(2.0) * cfg.RatedVoltageLL; // ~565V for 400Vac
            State.BusVoltage = VdcNom;
            State.HeatsinkTemp = cfg.AmbientTemp;
            State.TargetFrequency = 0;
            State.OutputFrequency = 0;
        }

        public void SetTargetFrequency(double freqHz)
        {
            State.TargetFrequency = Math.Clamp(freqHz, 0, Cfg.RatedFrequency * 1.5);
        }

        public void ToggleAnomaly(string key, bool enable)
        {
            switch (key.ToLowerInvariant())
            {
                case "undervoltage": State.An_UnderVoltage = enable; break;
                case "overvoltage": State.An_OverVoltage = enable; break;
                case "phaseloss": State.An_PhaseLoss = enable; break;
                case "groundfault": State.An_GroundFault = enable; break;
                case "loadjam": motor.State.An_LoadJam = enable; break;
                case "bearingwear": motor.State.An_BearingWear = enable; break;
                case "sensornoise": motor.State.An_SensorNoise = enable; break;
                default: break;
            }
        }

        public void Step(double dt)
        {
            State.Time += dt;

            // Update DC bus based on anomalies (simple scaling)
            if (State.An_UnderVoltage) State.BusVoltage = VdcNom * 0.5; // sag
            else if (State.An_OverVoltage) State.BusVoltage = VdcNom * 1.25; // surge
            else State.BusVoltage = VdcNom;

            // Faults that trip instantly
            if (State.An_GroundFault) { State.Trip(FaultCode.GroundFault); }

            // If tripped, outputs are disabled (coast) but we keep thermal cooling
            if (!State.Running)
            {
                ThermalStep(dt, conductionLossW: 0.0);
                State.OutputFrequency = 0.0;
                State.OutputVoltage = 0.0;
                State.MotorCurrent = 0.0;
                return;
            }

            // Slew output frequency toward target
            double df = State.TargetFrequency - State.OutputFrequency;
            double slew = (df >= 0 ? Cfg.Accel : Cfg.Decel) * dt;
            if (Math.Abs(df) <= Math.Abs(slew)) State.OutputFrequency = State.TargetFrequency;
            else State.OutputFrequency += Math.Sign(df) * Math.Abs(slew);

            // V/f + boost, capped at rated voltage
            double vf = Math.Max(0.1, State.OutputFrequency);
            double voltCmd = Cfg.RatedVoltageLL * (vf / Math.Max(1.0, Cfg.RatedFrequency));
            double boost = Cfg.VoltBoost * Cfg.RatedVoltageLL;
            State.OutputVoltage = Math.Min(Cfg.RatedVoltageLL, voltCmd + boost);

            // Motor step -> get torque, current, slip
            var (T_e, I, slip, nsync) = motor.Step(dt, State.OutputVoltage, State.OutputFrequency, State.BusVoltage);
            State.MotorCurrent = I;

            // Thermal model (very rough): inverter losses ~ 2% of V*I plus cooling to ambient
            double lossesW = 0.02 * State.OutputVoltage * State.MotorCurrent;
            ThermalStep(dt, lossesW);

            // Fault detection
            DetectTrips();
        }

        private void ThermalStep(double dt, double conductionLossW)
        {
            // Simplified RC: dT/dt = (P/k - (T - Tamb)/tau)
            double tamb = Cfg.AmbientTemp;
            double k = 25.0; // W/°C (effective cooling constant for this toy model)
            double dT = (conductionLossW / k - (State.HeatsinkTemp - tamb) / Cfg.ThermalTimeConstant) * dt;
            State.HeatsinkTemp += dT;
        }

        private void DetectTrips()
        {
            // DC bus
            if (State.BusVoltage < Cfg.UnderVoltPUNomDC * Math.Sqrt(2.0) * Cfg.RatedVoltageLL)
                State.Trip(FaultCode.UnderVoltage);
            if (State.BusVoltage > Cfg.OverVoltPUNomDC * Math.Sqrt(2.0) * Cfg.RatedVoltageLL)
                State.Trip(FaultCode.OverVoltage);

            // Over-current
            if (State.MotorCurrent > Cfg.OverCurrentMultiple * Cfg.MaxCurrent)
                State.Trip(FaultCode.OverCurrent);

            // Over-temperature
            if (State.HeatsinkTemp > Cfg.MaxHeatsinkTemp)
                State.Trip(FaultCode.OverTemp);

            // Phase loss is a latched trip in many drives; we do it if it persists
            if (State.An_PhaseLoss && State.Time % 0.5 < 1e-3)
                State.Trip(FaultCode.PhaseLoss);
        }
    }

    #endregion

    #region Scenario & Simulation

    public abstract class SimEvent
    {
        public double AtTimeSec;
        public abstract void Apply(Vfd vfd);
        public override string ToString() => $"{GetType().Name}@{AtTimeSec:F2}s";
    }

    public class SetFreqEvent : SimEvent
    {
        public double TargetHz;
        public override void Apply(Vfd vfd)
        {
            vfd.SetTargetFrequency(TargetHz);
            vfd.State.EventLog.Add($"[{vfd.State.Time,6:F2}s] Set target f = {TargetHz:F1} Hz");
        }
    }

    public class ToggleAnomalyEvent : SimEvent
    {
        public string Key = string.Empty; // e.g., "undervoltage", "phaseloss", "loadjam", ...
        public bool Enable;
        public override void Apply(Vfd vfd)
        {
            vfd.ToggleAnomaly(Key, Enable);
            vfd.State.EventLog.Add($"[{vfd.State.Time,6:F2}s] {(Enable ? "EN" : "DIS")}ABLE anomaly '{Key}'");
        }
    }

    public class ResetTripEvent : SimEvent
    {
        public override void Apply(Vfd vfd) => vfd.State.ResetTrip();
    }

    public static class Table
    {
        public static void PrintHeader()
        {
            Console.WriteLine("time    f_out  V_out  I(A)   rpm     T(Nm)  T_hs(°C) Vdc   RUN Trip");
            Console.WriteLine(new string('-', 78));
        }

        public static void PrintRow(Vfd vfd, InductionMotor motor)
        {
            var st = vfd.State; var ms = motor.State;
            Console.WriteLine(
                $"{st.Time,6:F2}  {st.OutputFrequency,5:F1}  {st.OutputVoltage,5:F0}  {st.MotorCurrent,5:F1}  {ms.SpeedRpm,6:F0}  {ms.ElectTorque,6:F1}  {st.HeatsinkTemp,6:F1}  {st.BusVoltage,4:F0}  {(st.Running ? "Y" : "N")}   {st.ActiveTrip}");
        }
    }

    public class Simulator
    {
        public readonly Vfd Vfd;
        public readonly InductionMotor Motor;
        public readonly List<SimEvent> Events;

        public Simulator(Vfd vfd, InductionMotor motor, List<SimEvent> events)
        {
            Vfd = vfd; Motor = motor; Events = events;
            Events.Sort((a, b) => a.AtTimeSec.CompareTo(b.AtTimeSec));
        }

        public void Run(double totalTimeSec, double dt, double samplePeriod)
        {
            double nextSample = 0.0;
            int idx = 0;
            Table.PrintHeader();

            while (Vfd.State.Time < totalTimeSec)
            {
                // Fire due events
                while (idx < Events.Count && Events[idx].AtTimeSec <= Vfd.State.Time + 1e-9)
                {
                    Events[idx].Apply(Vfd);
                    idx++;
                }

                Vfd.Step(dt);

                if (Vfd.State.Time >= nextSample)
                {
                    Table.PrintRow(Vfd, Motor);
                    nextSample += samplePeriod;
                }
            }

            // Print event log at the end
            Console.WriteLine();
            Console.WriteLine("Event log:");
            foreach (var e in Vfd.State.EventLog)
                Console.WriteLine(" - " + e);
        }
    }

    #endregion

    public static class Program
    {
        // Expose Vfd instance for motor to check phase loss flag (toy coupling)
        public static Vfd? Vfd;

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // --- Build motor & VFD ---
            var mParams = new MotorParams();
            var mState = new MotorState { SpeedRpm = 0 };
            var motor = new InductionMotor(mParams, mState);

            var vfdCfg = new VfdSettings();
            var vfdState = new VfdState();
            Vfd = new Vfd(vfdCfg, vfdState, motor);

            // --- Scenario ---
            var scenario = new List<SimEvent>
            {
                new SetFreqEvent{ AtTimeSec = 0.0, TargetHz = 50.0 },

                // Inject a load jam at 5s for 7s
                new ToggleAnomalyEvent{ AtTimeSec = 5.0, Key = "loadjam", Enable = true },
                new ToggleAnomalyEvent{ AtTimeSec = 12.0, Key = "loadjam", Enable = false },

                // Under-voltage sag at 15s
                new ToggleAnomalyEvent{ AtTimeSec = 15.0, Key = "undervoltage", Enable = true },
                new ToggleAnomalyEvent{ AtTimeSec = 16.5, Key = "undervoltage", Enable = false },

                // Phase loss at 20s (likely trips soon)
                new ToggleAnomalyEvent{ AtTimeSec = 20.0, Key = "phaseloss", Enable = true },

                // Attempt auto reset at 22.0s so we can continue
                new ResetTripEvent{ AtTimeSec = 22.0 },
                new ToggleAnomalyEvent{ AtTimeSec = 22.0, Key = "phaseloss", Enable = false },

                // Bearing wear after 24s (persistent)
                new ToggleAnomalyEvent{ AtTimeSec = 24.0, Key = "bearingwear", Enable = true },
            };

            var sim = new Simulator(Vfd, motor, scenario);

            // --- Run ---
            double totalTime = 30.0; // s
            double dt = 0.01;        // s integration step
            double sample = 0.5;     // s print period
            sim.Run(totalTime, dt, sample);

            Console.WriteLine("\nDone.");
        }
    }
}
