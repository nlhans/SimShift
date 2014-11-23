using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Data.Common;

namespace SimShift.Services
{
    class SteerngSmoothing : IControlChainObj
    {
        public bool Requires(JoyControls c)
        {
            return JoyControls.Steering == c;
        }

        private double lastSteer = 0.5;
        private double lastSpeed = 0;
        public double GetAxis(JoyControls c, double val)
        {
            if (c == JoyControls.Steering)
            {
                var a = 0.075;
                a =0.05+ Math.Abs(val - lastSteer);
                val = val*a + lastSteer*(1 - a);
                lastSteer = val;

                // Scale value with speed
                if (lastSpeed > 30)
                {
                    val -= 0.5;
                    val *= 1 - (lastSpeed - 30)/200;
                    val += 0.5;
                }
            }
            return val;
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
            Enabled = true;
            Active = Math.Abs(lastSteer - 0.5) > 0.01;
        }

        public void TickTelemetry(IDataMiner data)
        {
            lastSpeed = data.Telemetry.Speed*3.6;
        }

        public bool Enabled { get; private set; }
        public bool Active { get; private set; }
    }
}
