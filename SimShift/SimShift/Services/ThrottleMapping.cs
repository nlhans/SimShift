using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data.Common;

namespace SimShift.Services
{
    class ThrottleMapping : IControlChainObj
    {
        private double Rpm = 0;
        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            return (c == JoyControls.Throttle);
        }

        public double GetAxis(JoyControls c, double val)
        {
            var amp = 2;
            var expMax = Math.Exp(1 * amp) - 1;
            //if (Rpm > 1580)
            //    return val * (1 - (Rpm - 1580) / 250);
            if (c == JoyControls.Throttle)
                return (Math.Exp(val*amp)-1)/expMax;
            else
                return val;
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
            Enabled = true;
            Active = true;

            Rpm = data.Telemetry.EngineRpm;
        }

        public bool Enabled { get; private set; }
        public bool Active { get; private set; }

        #endregion
    }
}
