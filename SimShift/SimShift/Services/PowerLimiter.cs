using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public class PowerLimiter : IControlChainObj
    {
        public bool Enabled { get; private set; }

        public bool Active { get; private set; }

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
            if (data.Telemetry.Car == "iveco.hiway" && Main.CarProfile.Active != "Race" && Main.CarProfile.Active != "Performance")// && !Main.Data.Active.SelectManually)
            {
                Enabled = true;

                // V8 truck with lots of powerr
                if (Main.CruiseControl.Cruising && !Main.CruiseControl.ManualOverride)
                {
                    Active = false;
                    throttleFactor = 1;
                }
                else
                {
                    Active = true;
                    var pwrLimit = 320 + data.Weight / 1000.0 * 12.5;
                    //pwrLimit += 2500;
                    //if (data.Telemetry.Gear >= 7)
                    if (Main.Data.Telemetry.Gear >= 11) pwrLimit += 155;
                    var thrFactor = Main.Drivetrain.CalculateThrottleByPower(data.Telemetry.EngineRpm, pwrLimit);
                    //var thrFactor = Main.Drivetrain.CalculateThrottleByTorque(data.Telemetry.EngineRpm, 5000);
                    if (thrFactor > 1) thrFactor = 1;
                    if (thrFactor < 0.1) thrFactor = 0.1;
                    throttleFactor = thrFactor;
                }
            }else
            {
                throttleFactor = 1;
                Active = false;                
            }
        }

        #endregion
    }
}
