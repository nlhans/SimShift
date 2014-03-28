using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Utils;

namespace SimShift.Services
{
    public class CruiseControl : IControlChainObj, IConfigurable
    {
        public bool Cruising { get; private set; }
        public double Speed { get; private set; }
        public double SpeedCruise { get; private set; }

        private DateTime CruiseTimeout { get; set; }

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    case JoyControls.Brake:
                    return Cruising;

                case JoyControls.CruiseControl:
                    return true;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    var t = Cruising ? Math.Max(val, (SpeedCruise - Speed)*3.6*Slope) : val;
                    if (t > 1) t = 1;
                    if (t < 0) t = 0;
                    return t;
                case JoyControls.Brake:
                    if (val > 0.1)
                        Cruising = false;
                    return val;

                default:
                    return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch(c)
            {
                case JoyControls.CruiseControl:
                    if (val && DateTime.Now.Subtract(CruiseTimeout).TotalMilliseconds > 500)
                    {
                        Cruising = !Cruising;
                        SpeedCruise = Speed;
                        Debug.WriteLine("Cruising set to " + Cruising);
                        CruiseTimeout = DateTime.Now;
                    }
                    return false;
                    break;

                default:
                    return val;
            }
        }

        public void TickTelemetry(IDataMiner data)
        {
            Speed = data.Telemetry.Speed;
        }

        public void TickControls()
        {
        }

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs { get { return new[] {"Cruise"}; } }
        public void ResetParameters()
        {
            Slope = 0.25;
        }

        public double Slope { get; private set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "Slope":
                case "P":
                    Slope = obj.ReadAsFloat();
                    break;

                    // TODO: implement PID
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> o = new List<IniValueObject>();
            o.Add(new IniValueObject(AcceptsConfigs, "Slope", Slope.ToString("0.0000")));
            return o;
        }

        #endregion
    }
}
