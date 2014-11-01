using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Utils;

namespace SimShift.Services
{
    public class Speedlimiter : IControlChainObj, IConfigurable
    {
        public bool Active { get { return limiterFactor < 0.99; } }

        public int SpeedLimit { get; private set; }
        public float SpeedSlope { get; private set; }
        public bool Enabled { get; private set; }

        private bool fuelTest = false;
        private float fuelRpm = 500;
        private DateTime fuelLastChange = DateTime.Now;
        private double limiterFactor;

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    return true;

                case JoyControls.Brake:
                    return fuelTest;
                case JoyControls.CruiseControlUp:
                case JoyControls.CruiseControlDown:
                    return true;
                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    return fuelTest ? 1 : val*this.limiterFactor;
                    break;

                    case JoyControls.Brake:
                    return fuelTest? 0.5*brakeFactor:val;
                    
                default:
                    return val;
            }
        }

        private double brakeFactor;

        public bool GetButton(JoyControls c, bool val)
        {
            if (c == JoyControls.CruiseControlUp && Main.GetButtonIn(c))
            {
                if (DateTime.Now.Subtract(fuelLastChange).TotalMilliseconds > 500)
                {
                    fuelRpm += 100;
                    fuelLastChange = DateTime.Now;
                }
            }
            if (c == JoyControls.CruiseControlDown && Main.GetButtonIn(c))
            {
                if (DateTime.Now.Subtract(fuelLastChange).TotalMilliseconds > 500)
                {
                    fuelRpm -= 100;
                    fuelLastChange = DateTime.Now;
                }
            }
            return val;
        }

        public void TickControls()
        {
        }

        private double integralBrake = 0;
        public void TickTelemetry(IDataMiner data)
        {
            SpeedLimit = 125;
            if (fuelTest)
            {
                Enabled = true;
                SpeedLimit = 10;
                SpeedSlope = 2.5f;
                var rpmLimit = fuelRpm;

                var e = (data.Telemetry.EngineRpm - rpmLimit);
                integralBrake += e / 250 * 0.0025;
                if (integralBrake > 2.5) integralBrake = 2.5;
                if (integralBrake < 0) integralBrake = 0;
                //brakeFactor = (data.Telemetry.Speed * 3.6 - SpeedLimit) / SpeedSlope;
                brakeFactor = e / 750 + integralBrake;
            }
            if (!Enabled)
            {
                limiterFactor = 1;
            }
            else
            {
                limiterFactor = 1 + (SpeedLimit - data.Telemetry.Speed*3.6)/SpeedSlope;

                if (limiterFactor < 0) limiterFactor = 0;
                if (limiterFactor > 1) limiterFactor = 1;
            }
        }

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs { get { return new[] {"Speedlimit"}; } }

        public void ResetParameters()
        {
            SpeedLimit = 255;
            SpeedSlope = 10;
            Enabled = true;
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
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
