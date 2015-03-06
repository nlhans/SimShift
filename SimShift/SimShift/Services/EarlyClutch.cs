using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    /// This module is a gimmick where the clutch is engaged when dropping below 35km/h when the vehicle was driving faster than that (55km/h+)
    /// </summary>
    class EarlyClutch : IControlChainObj
    {
        public bool Enabled { get; private set; }
        public bool Active { get; private set; }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        //
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
            //clutching = Main.GetAxisIn(JoyControls.Throttle) < 0.1 && clutchctrl;
            clutching = false;
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

        #endregion
    }
}
