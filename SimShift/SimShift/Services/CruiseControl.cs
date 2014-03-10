using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SimShift.Data;

namespace SimShift.Services
{
    public class CruiseControl : IControlChainObj
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
var t = Cruising ? Math.Max(val, (SpeedCruise - Speed)*3.6*0.25) : val;
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

        public void TickTelemetry(Ets2DataMiner data)
        {
            Speed = data.Telemetry.speed;
        }

        public void TickControls()
        {
        }
    }
}
