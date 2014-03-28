using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public class Speedlimiter : IControlChainObj
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
        {//
        }

        public void TickTelemetry(IDataMiner data)
        {
            limiterFactor = 1 + (95 - data.Telemetry.Speed * 3.6) / 12;
            limiterFactor *= Math.Max(0, Math.Min(1, 1 - (data.Telemetry.EngineRpm - 1500) / 700));

            if (limiterFactor < 0) limiterFactor = 0;
            if (limiterFactor > 1) limiterFactor = 1;
        }
    }
}
