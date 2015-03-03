using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    /// Traction Control limits the wheel slip via open-loop control by reducing throttle when a slip % exceeds predetermined offset.
    /// The aggressiveness is adjusted by slope.
    /// Optionally a rattling sound can be played to alert the driver TC is overriding control.
    /// </summary>
    public class TractionControl : IControlChainObj, IConfigurable
    {
        public bool Enabled { get
        {
            return AllowedSlip < 30;
        } }
        public bool Active { get { return Slipping; } }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        //
        private SoundPlayer tcSound;

        public bool Slipping { get; private set; }
        public double WheelSpeed { get; private set; }
        public double EngineSpeed { get; private set; }
        public double SlipAngle { get; private set; }

        private double lastThrottle = 0;

        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        private DateTime lastPlay = DateTime.Now;
        public bool CanPauseTrack {get { return DateTime.Now > lastPlay; }}
        public bool SoundStopped { get; private set; }

        public TractionControl()
        {
            tcSound = new SoundPlayer(@"C:\Projects\Software\SimShift\Resources\tractioncontrol.wav");
            setVolume(0);
            SoundStopped = true;
            lastThrottle = 1;
            setVolume(1);
            var updateSound = new Timer {Enabled = true, Interval = 10};
            updateSound.Elapsed += (sender, args) =>
                                       {
                                           if (Main.Transmission.IsShifting || Antistall.Stalling || !Slipping)
                                               setVolume(0);
                                           else
                                               setVolume(1 - lastThrottle);
                                       };
        updateSound.Start();
        }

        private void setVolume(double vol)
        {
            if (vol == 0)
            {
                if (SoundStopped) return;
                if (CanPauseTrack)
                {
                    vol = 1;
                    tcSound.Stop();
                    SoundStopped = true;
                }
            }
            else
            {
                lastPlay = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
                if (SoundStopped)
                {
                    tcSound.PlayLooping();
                    SoundStopped = false;
                }
            }

            uint vol_hex = (uint) (vol * 0x7FFF);
            uint vol_out = vol_hex | (vol_hex << 16);
            //vol_out = 0xFFFFFFFF;
            waveOutSetVolume(IntPtr.Zero, vol_out);
        }

        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                default:
                    return false;

                case JoyControls.Throttle:
                    return Slipping;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                default:
                    return val;

                case JoyControls.Throttle:
                    var t = (1 - (SlipAngle - 1 - AllowedSlip)/Slope);
                    if (t > 1) t = 1;
                    if (t < 0) t = 0;
                    lastThrottle =t * 0.05 + lastThrottle*0.95;
                    return val*lastThrottle;
                    break;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
            //
        }

        public void TickTelemetry(IDataMiner data)
        {
            try
            {
                WheelSpeed = Main.Drivetrain.CalculateSpeedForRpm(data.Telemetry.Gear - 1, data.Telemetry.EngineRpm);
                EngineSpeed = data.Telemetry.Speed;//the actual speed we are going
                if (double.IsInfinity(WheelSpeed)) WheelSpeed = 0;
                if (Antistall.Stalling) WheelSpeed = 0;
                SlipAngle = WheelSpeed / EngineSpeed;
                Slipping = (SlipAngle - AllowedSlip > 1.0);
                if (Main.Drivetrain.CalculateSpeedForRpm(data.Telemetry.Gear - 1, (float)Main.Drivetrain.StallRpm * 1.2f) >= data.Telemetry.Speed)
                {
                    Slipping = false;
                }
            }catch
            {
            }
        }

        #endregion

        #region Implementation of IConfigurable

        public string File { get; set; }
        public double AllowedSlip { get; private set; }
        public double Slope { get; private set; }

        public IEnumerable<string> AcceptsConfigs { get { return new string[] {"TractionControl"}; } }

        public void ResetParameters()
        {
            AllowedSlip = 5;
            Slope = 5;
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "Slope":
                    Slope = obj.ReadAsFloat();
                    break;

                case "Slip":
                    AllowedSlip = obj.ReadAsFloat();
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();

            obj.Add(new IniValueObject(AcceptsConfigs, "Slope", Slope.ToString()));
            obj.Add(new IniValueObject(AcceptsConfigs, "Slip", AllowedSlip.ToString()));

            return obj;
        }

        #endregion
    }
}
