using System;
using SimShift.Data;

namespace SimShift.Services
{
    public class Antistall : IControlChainObj
    {
        public bool Stalling { get; private set; }
        public double Speed { get; private set; }

        private double _throttle;

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                case JoyControls.Clutch:
                    return Stalling;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (val > 0.025)
                    {
                        _throttle = val;
                        return val/3;
                    }
                    else
                    {
                        _throttle = 0;
                        return 0;
                    }

                case JoyControls.Clutch:
                    return 1 - _throttle*0.8;

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
            //
        }

        public void TickTelemetry(Ets2DataMiner telemetry)
        {
            Stalling = (telemetry.Telemetry.speed < 1);
            Speed = telemetry.Telemetry.speed;
        }
    }
}