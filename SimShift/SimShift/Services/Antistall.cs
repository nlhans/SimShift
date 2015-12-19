using System;
using System.Collections.Generic;
using System.Diagnostics;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    /// This module is the "Auto-Clutch" feature and engages the clutch when the engine is about to stall.
    /// It also ensures smooth get-away when the user engages throttle when the vehicle has stopped.
    /// </summary>
    public class Antistall : IControlChainObj, IConfigurable
    {
        public bool Enabled { get; set; }
        public bool Active { get { return Stalling; } }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }


        //
        public static bool Stalling { get; private set; }
        public double Speed { get; private set; }

        public double Rpm { get; private set; }

        private double _throttle;

        public bool SlipLowGear { get; private set; }
        private bool SlippingLowGear { get; set; }

        protected bool EngineStalled { get; set; }

        public bool ReverseAndAccelerate { get; private set; }

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
                    return Enabled && Stalling;
                    
                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (ReverseAndAccelerate)
                    {
                        return 0;
                    }
                    _throttle = val;
                    return val;
                    if (EngineRpm > AntiStallRpm)
                        val /= 5*(EngineRpm-AntiStallRpm)/AntiStallRpm;

                    _throttle = val;
                    return val;
                    if (Override)
                        return 1;
                    if (!Stalling)
                        return val;
                    if (EngineStalled)
                    {
                        _throttle = val;
                        return val;
                    }
                    if (val < 0.01)
                    {
                        _throttle = 0;
                        return 0;
                    }
                    else
                    {
                        var maxRpm = Main.Drivetrain.StallRpm*1.4;
                        var maxV = 2 -  2*Rpm/(maxRpm);
                        if (maxV > 1) maxV = 1;
                        if (maxV < 0) maxV = 0;
                        _throttle = val;
                        return maxV;
                    }
                    break;

                case JoyControls.Clutch:
                    if (ReverseAndAccelerate)
                    {
                        return 1;
                    }
                    if (Stalling && _throttle < 0.01)
                    {
                        if (SpeedCutoff >= Speed)
                            return 1;
                        else
                            return 1- (Speed-SpeedCutoff)/0.5f;
                    }

                    var cl = 1 - _throttle*ThrottleSensitivity; // 2
                    if (cl < MinClutch) cl = MinClutch; // 0.1
                    cl = Math.Max(cl, val);

                    if (EngineRpm < AntiStallRpm)
                        cl += (AntiStallRpm - EngineRpm) / AntiStallRpm;

                    return cl;
                    
                default:
                    return val;
            }
        }

        public double EngineRpm { get; set; }

        public bool Override { get; set; }

        private double AntiStallRpm;
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
            AntiStallRpm = Main.Drivetrain.StallRpm*1.25f;

            var gear = telemetry.Telemetry.Gear - 1;
            if (gear == -2) gear = 0;
            if (gear == 0) gear = 1;
            SpeedCutoff = Main.Drivetrain.CalculateSpeedForRpm(gear, (float)AntiStallRpm);

            if (telemetry.Telemetry.Gear == -1)
            {
                ReverseAndAccelerate = telemetry.Telemetry.Speed > 0.5;
                Stalling = telemetry.Telemetry.Speed+0.25 >= -SpeedCutoff;
            }
            else
            {
                ReverseAndAccelerate = telemetry.Telemetry.Speed < -0.5;
                Stalling = telemetry.Telemetry.Speed-0.25 <= SpeedCutoff;
            }
            Stalling |= ReverseAndAccelerate;

            Speed = telemetry.Telemetry.Speed;
            EngineRpm = telemetry.Telemetry.EngineRpm;

            if (EngineStalled && _throttle > 0)
            {
                Override = true;
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
            MinClutch = 0.1;
            ThrottleSensitivity = 2;
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "MinClutch":
                    MinClutch = obj.ReadAsDouble();
                    break;
                case "ThrottleSensitivity":
                    ThrottleSensitivity = obj.ReadAsDouble();
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            var group = new List<string>(new [] { "Antistall" });
            var parameters = new List<IniValueObject>();
            
            parameters.Add(new IniValueObject(group, "MinClutch", SpeedCutoff.ToString("0.00")));
            parameters.Add(new IniValueObject(group, "ThrottleSensitivity", SpeedCutoff.ToString("0.00")));

            return parameters;
        }
        #endregion
    }
}