using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    /// This module can make the throttle less sensitive. This is adventagous when driving some vehicles, like trucks.
    /// </summary>
    class ThrottleMapping : IControlChainObj
    {
        public bool Enabled { get; private set; }
        public bool Active { get; private set; }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        //
        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            return (c == JoyControls.Throttle);
        }

        public double GetAxis(JoyControls c, double val)
        {
            var amp = 2;
            var expMax = Math.Exp(1 * amp) - 1;

            if (c == JoyControls.Throttle)
                return (Math.Exp(val * amp) - 1) / expMax;
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
        }

        #endregion
    }
}
