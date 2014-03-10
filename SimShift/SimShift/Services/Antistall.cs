using System;
using System.Diagnostics;
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
                        return val/1;
                    }
                    else
                    {
                        _throttle = 0;
                        return 0;
                    }
                    break;

                case JoyControls.Clutch:
                    var cl = 1 - _throttle*3;
                    if(cl<0.1) cl = 0.1;
                    return cl;

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
            var stallRpm = Main.Transmission.GetActiveConfiguration().Engine.StallRpm;
            var calculatedEngineRpmBySpeed =
                Main.Transmission.GetActiveConfiguration().RpmForSpeed(telemetry.Telemetry.speed,
                                                                       telemetry.Telemetry.gear);
            if (calculatedEngineRpmBySpeed < 700)
            {
                //Debug.WriteLine("Stalling {0:0000} / {1:0000}", calculatedEngineRpmBySpeed, telemetry.Telemetry.engineRpm);
            }
            Stalling = (telemetry.Telemetry.speed < 1);// || calculatedEngineRpmBySpeed < stallRpm;
            Speed = telemetry.Telemetry.speed;
        }
    }
}