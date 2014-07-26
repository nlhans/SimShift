using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data.Common;

namespace SimShift.Services
{
    class EarlyClutch : IControlChainObj
    {
        private bool clutching = false;
        private bool clutchctrl = false;

        private bool triggered = false;
        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Clutch:
                    return clutching;
                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch(c)
            {
                case JoyControls.Clutch:
                    return clutching ? 1 : val;
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
            clutching = Main.GetAxisIn(JoyControls.Throttle) < 0.1 && clutchctrl;
        }

        public void TickTelemetry(IDataMiner data)
        {
            if (data.Telemetry.Speed * 3.6 > 55)
                triggered = true;

            if (triggered && data.Telemetry.Speed * 3.6  < 35)
            {
                clutchctrl = true;
            }
            else if (data.Telemetry.Speed*3.6 > 35)
            {
                clutchctrl = false;
            }
            if (data.Telemetry.Speed*3.6 < 10 && Main.GetAxisIn(JoyControls.Throttle) > 0.1)
            {
                clutchctrl = false;
                triggered = false;
            }
        }

        public bool Enabled { get; private set; }
        public bool Active { get; private set; }

        #endregion
    }
}
