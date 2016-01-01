using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SimShift.Data.Common;
using SimShift.Dialogs;
using SimShift.Entities;

namespace SimShift.Services
{
    public class ACC: IControlChainObj
    {
        public bool Enabled { get { return true; } }
        public bool Active { get { return Cruising; } }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        public bool Cruising { get; private set; }
        public double Speed { get; private set; }
        public double SpeedCruise { get; private set; }

        private DateTime CruiseTimeout { get; set; }

        private float t, b;

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
                    if (Cruising)
                        return Math.Min(1,Math.Max(0,t));
                    else
                        return val;
                case JoyControls.Brake:
                    if (Cruising)
                        return Math.Min(1, Math.Max(0, b));
                    else
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

            if (Main.GetAxisIn(JoyControls.Brake) > 0.05)
                Cruising = false;

            // Any tracked car?
            if (dlDebugInfo.TrackedCar != null)
            {
                // Gap control?
                var distanceTarget = 20;
                var distanceError = distanceTarget - dlDebugInfo.TrackedCar.Distance;

                var speedBias = 9*distanceError/distanceTarget; // 3m/s max decrement
                if (distanceError < 0)
                    speedBias /= 6;
                var targetSpeed = dlDebugInfo.TrackedCar.Speed - speedBias;

                if (targetSpeed >= SpeedCruise)
                    targetSpeed = (float)SpeedCruise;

                var speedErr = data.Telemetry.Speed - targetSpeed-2;
                if (speedErr > 0) // too fast
                {
                    t = 0;
                    if (speedErr>1.5f)
                        b = (float) Math.Pow(speedErr-1.5f,4)*0.015f;
                }
                else
                {
                    t = -speedErr*0.2f;
                    b = 0;
                }
            }
            else
            {
                // Speed control
                var speedErr = data.Telemetry.Speed - (float)SpeedCruise;
                if (speedErr > 0) // too fast
                {
                    t = 0;
                }
                else
                {
                    t = -speedErr * 0.4f;
                    b = 0;
                }
            }
        }


        public void TickControls()
        {
        }

    }
}
