using System;
using System.Collections.Generic;
using System.Diagnostics;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Utils;

namespace SimShift.Services
{
    public class Antistall : IControlChainObj, IConfigurable
    {
        public bool Enabled { get; set; }
        public static bool Stalling { get; private set; }
        public double Speed { get; private set; }

        public double Rpm { get; private set; }

        private double _throttle;

        public DateTime TimeStopped { get; private set; }
        public DateTime TimeStalled { get; set; }

        public bool SlipLowGear { get; private set; }
        private bool SlippingLowGear { get; set; }

        public bool Blip { get; private set; }
        protected bool EngineStalled { get; set; }

        public bool Override { get; private set; }

#region Configurable parametrs
        public double MinClutch { get; private set; }
        public double SpeedCutoff { get; private set; }
        public double ThrottleSensitivity { get; private set; }
#endregion

        public Antistall()
        {
            Enabled = true;
        }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    return Enabled&& Stalling;
                    
                case JoyControls.Clutch:
                    return Enabled && (Stalling || SlippingLowGear);
                    
                default:
                    return false;
            }
        }

        private int tick = 0;
        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (!Stalling) return val;
                    if (EngineStalled)
                    {
                        _throttle = 0;
                        return val; }
                    if (BlipFull) return 1;
                    if (Override) return 0;
                    if (val < 0.025)
                    {
                        _throttle = 0;
                        tick++;

                        return 1 - Rpm / (700 + Math.Sin(tick * 2 * Math.PI / 80.0) * 50 + (Blip ? 1300 : 0));
                    }
                    else
                    {
                        var maxRpm = Main.Drivetrain.MaximumRpm/2;
                        if (maxRpm < 2000) maxRpm = 2000;
                        var maxV = 2 - 2*Rpm/(maxRpm);
                        if (maxV > 1) maxV = 1;
                        if (maxV < 0) maxV = 0;
                        _throttle = val;
                        return maxV;
                    }
                    break;

                case JoyControls.Clutch:
                    if (!Stalling && !SlippingLowGear) return 0;
                    if (Blip || BlipFull) return 1;
                    if (Override) return 1;

                    if (Stalling)
                    {
                        var cl = 1 - _throttle*ThrottleSensitivity; // 2
                        if (cl < MinClutch) cl = MinClutch; // 0.1
                        return cl;
                    }
                    else if(SlippingLowGear)
                    {
                         var t =1 - 1.3*(Rpm-1500)/500;
                        t = Math.Max(val, Math.Min(1, Math.Max(0, t)));
                        return t;
                    }else
                    {
                        return 0;
                    }

                default:
                    return val;
            }
        }

        protected bool BlipFull;

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
            //
        }

        public void TickTelemetry(IDataMiner telemetry)
        {
            bool wasStalling = Stalling;
            bool wasEngineStalled = EngineStalled;

            Rpm = telemetry.Telemetry.EngineRpm;
            EngineStalled = (telemetry.Telemetry.EngineRpm < 300);
            if(telemetry.Telemetry.Gear == -1)
                Stalling = telemetry.Telemetry.Speed > -SpeedCutoff || telemetry.Telemetry.Speed>0;
            else
                Stalling = telemetry.Telemetry.Speed < SpeedCutoff || telemetry.Telemetry.Speed<0;
            //Stalling = (Math.Abs(telemetry.Telemetry.Speed) < SpeedCutoff); // 2
            Speed = telemetry.Telemetry.Speed;

            if (Main.CarProfile == null || true)
                SlippingLowGear = false;
            else
            SlippingLowGear = Main.CarProfile.Active.ToLower().Contains("performance") &&
                              Main.Transmission.ShiftCtrlNewGear <= 6;

            if(telemetry.EnableWeirdAntistall==false)
            {
                Blip = false;
                Override = false;
                BlipFull = false;
                return;
            }

            if (Stalling && !wasStalling)
            {
                TimeStopped = DateTime.Now;
            }
            if (!EngineStalled && wasEngineStalled)
            {
                TimeStalled = DateTime.Now;
            }

            if (!EngineStalled)
            {
                var dt = DateTime.Now.Subtract(TimeStalled).TotalMilliseconds;
                Blip = false;
                if (dt < 2500)
                    BlipFull = true;
                if(Rpm > 2000)
                {
                    BlipFull = false;
                    TimeStalled = DateTime.MinValue;
                }
                if(dt<3500)
                {
                    Override = true;
                }else
                {
                    Override = false;
                    if (Stalling && DateTime.Now.Subtract(TimeStopped).TotalMilliseconds % 43000 > 12500 &&
                        DateTime.Now.Subtract(TimeStopped).TotalMilliseconds % 43000 < 13500)
                    {
                        Blip = true;
                    }
                    else
                    {
                        Blip = false;
                        BlipFull = false;
                    }
                }
            }
            else
            {
                Override = false;
            }

        }

        #region Configurable parameters management

        public IEnumerable<string> AcceptsConfigs { get { return new[] {"Antistall"}; } }


        public void ResetParameters()
        {
            // Reset to default
            Speed = 2;
            MinClutch = 0.1;
            ThrottleSensitivity = 2;
            SlipLowGear = true;
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "Speed":
                    SpeedCutoff = obj.ReadAsDouble();
                    break;
                case "MinClutch":
                    MinClutch = obj.ReadAsDouble();
                    break;
                case "ThrottleSensitivity":
                    ThrottleSensitivity = obj.ReadAsDouble();
                    break;

                case "SlipLowGear":
                    SlipLowGear = obj.ReadAsString() == "yes";
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            var group = new List<string>(new [] { "Antistall" });
            var parameters = new List<IniValueObject>();
            
            parameters.Add(new IniValueObject(group, "Speed", SpeedCutoff.ToString("0.00")));
            parameters.Add(new IniValueObject(group, "MinClutch", SpeedCutoff.ToString("0.00")));
            parameters.Add(new IniValueObject(group, "ThrottleSensitivity", SpeedCutoff.ToString("0.00")));
            parameters.Add(new IniValueObject(group, "SlipLowGear", SlipLowGear ? "yes" : "no"));

            return parameters;
        }
        #endregion
    }
}