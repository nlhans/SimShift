using System;
using System.Diagnostics;
using SimShift.Data;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public class Antistall : IControlChainObj
    {
        public bool Stalling { get; private set; }
        public double Speed { get; private set; }

        public double Rpm { get; private set; }

        private double _throttle;

        public DateTime TimeStopped { get; private set; }
        public DateTime TimeStalled { get; set; }

        public bool Blip { get; private set; }
        protected bool EngineStalled { get; set; }

        public bool Override { get; private set; }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                case JoyControls.Clutch:
                    return Stalling;

                default:
                    return false;
            }
        }

        private int tick = 0;
        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (!Stalling) return val;
                    if (EngineStalled) return val;
                    if (BlipFull) return 1;
                    if (Override) return 0;
                    if (val < 0.025)
                    {
                        _throttle = 0;
                        tick++;
                        return 1 - Rpm / (850 + Math.Sin(tick * 2 * Math.PI / 40.0) * 45 + (Blip ? 1300 : 0));
                    }
                    else
                    {
                        var maxV = 1 - Rpm/1100.0;
                        if (maxV > 1) maxV = 1;
                        if (maxV < 0) maxV = 0;
                        _throttle = val;
                        return maxV;
                    }
                    break;

                case JoyControls.Clutch:
                    if (!Stalling) return 0;
                    if (Blip || BlipFull) return 1;
                    if (Override) return 1;

                    var cl = 1 - _throttle*2;
                    if (cl < 0.1) cl = 0.1;
                    return cl;

                default:
                    return val;
            }
        }

        protected bool BlipFull;

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
            //
        }

        public void TickTelemetry(IDataMiner telemetry)
        {
            bool wasStalling = Stalling;
            bool wasEngineStalled = EngineStalled;
            var stallRpm = Main.Transmission.GetActiveConfiguration().Engine.StallRpm;
            var calculatedEngineRpmBySpeed =
                Main.Transmission.GetActiveConfiguration().RpmForSpeed(telemetry.Telemetry.Speed,
                                                                       telemetry.Telemetry.Gear);
            if (calculatedEngineRpmBySpeed < 700)
            {
                //Debug.WriteLine("Stalling {0:0000} / {1:0000}", calculatedEngineRpmBySpeed, telemetry.MyTelemetry.engineRpm);
            }
            Rpm = telemetry.Telemetry.EngineRpm;
            EngineStalled = (telemetry.Telemetry.EngineRpm < 300);
            Stalling = (telemetry.Telemetry.Speed < 2); // || calculatedEngineRpmBySpeed < stallRpm;
            Speed = telemetry.Telemetry.Speed;

            if (Stalling && !wasStalling)
            {
                TimeStopped = DateTime.Now;
            }
            if (!EngineStalled && wasEngineStalled)
            {
                TimeStalled = DateTime.Now;
            }

            if (!EngineStalled)
            {
                var dt = DateTime.Now.Subtract(TimeStalled).TotalMilliseconds;
                Blip = false;
                if (dt < 2500)
                    BlipFull = true;
                if(Rpm > 2000)
                {
                    BlipFull = false;
                    TimeStalled = DateTime.MinValue;
                }
                if(dt<3500)
                {
                    Override = true;
                }else
                {
                    Override = false;
                    if (Stalling && DateTime.Now.Subtract(TimeStopped).TotalMilliseconds % 13000 > 2500 &&
                        DateTime.Now.Subtract(TimeStopped).TotalMilliseconds % 13000 < 3500)
                    {
                        Blip = true;
                    }
                    else
                    {
                        Blip = false;
                        BlipFull = false;
                    }
                }
            }
            else
            {
                Override = false;
            }

        }
    }
}