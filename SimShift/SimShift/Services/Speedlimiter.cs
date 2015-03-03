﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data;
using SimShift.Data.Common;
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
        public int SpeedLimit { get; private set; }
        public float SpeedSlope { get; private set; }

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
                    return val*this.limiterFactor;
                    break;

                    case JoyControls.Brake:
                    return brakeFactor:val;
                    
                default:
                    return val;
            }
        }

        private double brakeFactor;

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
        }

        private double integralBrake = 0;
        public void TickTelemetry(IDataMiner data)
        {
            SpeedLimit = 125;

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
