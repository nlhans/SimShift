using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public class PowerLimiter : IControlChainObj
    {
        private bool Active;
        private double throttleFactor = 1;

        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    return Active;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch( c)
            {
                case JoyControls.Throttle:
                    return val*throttleFactor;
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
            if(data.Telemetry.Car == "scania.r")
            {
                Active = true;

                // V8 truck with lots of powerr
                if (Main.CruiseControl.Cruising && !Main.CruiseControl.ManualOverride)
                    throttleFactor = 1;
                else
                {
                    var pwrLimit = 300 + data.Weight/1000.0*20;
                    //pwrLimit += 2500;
                    //if (data.Telemetry.Gear >= 7)
                        pwrLimit += data.Weight/1000.0*data.Telemetry.Gear;
                    var thrFactor = Main.Drivetrain.CalculateThrottleByPower(data.Telemetry.EngineRpm,pwrLimit);
                    if (thrFactor > 1) thrFactor = 1;
                    if (thrFactor < 0.2) thrFactor = 0.2;
                    throttleFactor = thrFactor;
                }
            }else
            {
                Active = false;                
            }
        }

        #endregion
    }
}
