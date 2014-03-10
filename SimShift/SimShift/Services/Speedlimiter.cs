using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data;

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

        public void TickTelemetry(Ets2DataMiner data)
        {
            limiterFactor = 1+(95 - data.Telemetry.speed*3.6)/20;

            if (limiterFactor < 0) limiterFactor = 0;
            if (limiterFactor > 1) limiterFactor = 1;
        }
    }
}
