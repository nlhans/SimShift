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
    /// <summary>
    /// This module simulates a CruiseControl. Although some games (like ETS2) incorporate Cruise Control, previously there were no features like resume CC, speed up or slow down CC
    /// And above all: the CC in-game disengages when shifting gear.
    /// </summary>
    public class CruiseControl : IControlChainObj, IConfigurable
    {
        public bool Enabled { get { return true; } }
        public bool Active { get { return Cruising && !ManualOverride; } }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }


        //
        public bool Cruising { get; private set; }
        public double Speed { get; private set; }
        public double SpeedCruise { get; private set; }

        private DateTime CruiseTimeout { get; set; }

        private double IntegralTime;
        private double PreviousError = 0;

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    case JoyControls.Brake:
                    return Cruising;

                    case JoyControls.CruiseControlMaintain:
                    case JoyControls.CruiseControlUp:
                    case JoyControls.CruiseControlDown:
                    case JoyControls.CruiseControlOnOff:
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
                    var error = SpeedCruise - Speed;
                    IntegralTime += error * ISlope;
                    var Differential = (error - PreviousError) * DSlope;
                    PreviousError = error;
                    if (IntegralTime > Imax) IntegralTime = Imax;
                    if (IntegralTime < -Imax) IntegralTime = -Imax;
                    var cruiseVal = error * 3.6*PSlope + IntegralTime+Differential;
                    ManualOverride = val >= cruiseVal;
                    if(Cruising && cruiseVal>val) val = cruiseVal;
                    var t = val;
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
                case JoyControls.CruiseControlMaintain:
                    if (val && DateTime.Now.Subtract(CruiseTimeout).TotalMilliseconds > 500)
                    {
                        Cruising = !Cruising;
                        SpeedCruise = Speed;
                        Debug.WriteLine("Cruising set to " + Cruising + " and " + SpeedCruise + " m/s");
                        CruiseTimeout = DateTime.Now;
                    }
                    return false;
                    break;

                    case JoyControls.CruiseControlUp:
                    if (val && DateTime.Now.Subtract(CruiseTimeout).TotalMilliseconds > 400)
                    {
                        SpeedCruise += 1/3.6f;
                        CruiseTimeout = DateTime.Now;
                    }
                    return false;
                    break;
                    case JoyControls.CruiseControlDown:
                    if (val && DateTime.Now.Subtract(CruiseTimeout).TotalMilliseconds > 400)
                    {
                        SpeedCruise -= 1/3.6f;
                        CruiseTimeout = DateTime.Now;
                    }
                    return false;
                    break;
                    case JoyControls.CruiseControlOnOff:
                    if (val && DateTime.Now.Subtract(CruiseTimeout).TotalMilliseconds > 500)
                    {
                        Cruising = !Cruising;
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
            PSlope = 0.25;
            ISlope = 0;
            Imax = 0;
            DSlope = 0;
        }

        public double PSlope { get; private set; }
        public double ISlope { get; private set; }
        public double Imax { get; private set; }
        public double DSlope { get; private set; }
        public bool ManualOverride { get; private set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "P":
                    PSlope = obj.ReadAsFloat();
                    break;
                case "Imax":
                    Imax = obj.ReadAsFloat();
                    break;
                case "I":
                    ISlope = obj.ReadAsFloat();
                    break;
                case "D":
                    DSlope = obj.ReadAsFloat();
                    break;

                    // TODO: implement PID
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> o = new List<IniValueObject>();
            o.Add(new IniValueObject(AcceptsConfigs, "P", PSlope.ToString("0.0000")));
            o.Add(new IniValueObject(AcceptsConfigs, "I", ISlope.ToString("0.0000")));
            o.Add(new IniValueObject(AcceptsConfigs, "Imax", Imax.ToString("0.0000")));
            o.Add(new IniValueObject(AcceptsConfigs, "D", DSlope.ToString("0.0000")));
            return o;
        }

        #endregion
    }
}
