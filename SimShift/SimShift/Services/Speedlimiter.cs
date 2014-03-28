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
        private double limiterFactor;

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
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
                    return val*limiterFactor;
                    break;

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

        public int SpeedLimit { get; private set; }
        public int SpeedSlope { get; private set; }
        public bool Enabled { get; private set; } 

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "Limit":
                    SpeedLimit = obj.ReadAsInteger();
                    break;

                case "Slope":
                    SpeedSlope = obj.ReadAsInteger();
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
