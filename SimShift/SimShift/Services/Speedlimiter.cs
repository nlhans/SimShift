using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using AForge.Imaging.Filters;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    /// Limits maximum speed of vehicle to avoid reckless driving, or create realism for some vehicles (eg 255km/h limit on german saloon cars).
    /// Maximum speed can be adjusted and must be set some km/h lower than the desired speed. Aggresiveness is determined by slope.
    /// </summary>
    public class Speedlimiter : IControlChainObj, IConfigurable
    {
        public bool Active { get { return limiterFactor < 0.99; } }
        public bool Enabled { get; private set; }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        //
        public bool Adaptive { get; private set; }
        public int SpeedLimit { get; private set; }
        public float SpeedSlope { get; private set; }

        private double brakeFactor;
        private double limiterFactor;

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    return true;
                    case JoyControls.Brake:
                    return brakeFactor > 0.005;
                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    return val*this.limiterFactor;
                case JoyControls.Brake:
                    return brakeFactor;

                default:
                    return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
        }

        public void TickTelemetry(IDataMiner data)
        {
            if (Adaptive && Main.Data.Active.Application == "eurotrucks2")
            {
                var ets2data = (Ets2DataMiner) Main.Data.Active;
                var ets2limit = ets2data.MyTelemetry.Job.SpeedLimit*3.6-4;
                if (ets2limit < 0)
                    ets2limit = 120;

                SpeedLimit = (int)ets2limit;
            }
            if (!Enabled)
            {
                brakeFactor = 0;
                limiterFactor = 1;
            }
            else
            {
                limiterFactor = 1 + (SpeedLimit - data.Telemetry.Speed*3.6)/SpeedSlope;

                if (limiterFactor < 0) limiterFactor = 0;
                if (limiterFactor > 1) limiterFactor = 1;


                if (data.Telemetry.Speed*3.6 - 5 >= SpeedLimit)
                {
                    var err = (data.Telemetry.Speed*3.6 - SpeedLimit)/25.0*0.25f;
                    brakeFactor = err;
                }
                else
                {
                    brakeFactor = 0;
                }
            }
            if (data.Telemetry.EngineRpm > 21750)
            {
                Enabled = true;
                limiterFactor *= Math.Max(0, 1 - (data.Telemetry.EngineRpm - 1750)/350.0f);
            }
            var pwrLimiter = Main.Drivetrain.CalculateThrottleByPower(data.Telemetry.EngineRpm, 800);

            if (pwrLimiter > 1) pwrLimiter = 1;
            if (pwrLimiter < 0.2) pwrLimiter = 0.2;

            limiterFactor *= pwrLimiter;
        }

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs { get { return new[] {"Speedlimit"}; } }

        public void ResetParameters()
        {
            SpeedLimit = 255;
            SpeedSlope = 10;
            Enabled = true;
            Adaptive = false;
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "Adaptive":
                    Adaptive = obj.ReadAsInteger() == 1;
                    break;

                case "Limit":
                    SpeedLimit = obj.ReadAsInteger();
                    break;

                case "Slope":
                    SpeedSlope = obj.ReadAsFloat();
                    break;

                case "Disable":
                    Enabled = false;
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> exportedObjects = new List<IniValueObject>();

            if(Enabled==false)
            {
                exportedObjects.Add(new IniValueObject(AcceptsConfigs, "Disable", "1"));
            }
            else
            {
                exportedObjects.Add(new IniValueObject(AcceptsConfigs, "Limit", SpeedLimit.ToString()));
                exportedObjects.Add(new IniValueObject(AcceptsConfigs, "Slope", SpeedSlope.ToString()));
            }
            return exportedObjects;
        }

        #endregion
    }
}
