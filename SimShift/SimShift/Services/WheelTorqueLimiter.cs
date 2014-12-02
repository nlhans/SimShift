using System;
using SimShift.Data.Common;

namespace SimShift.Services
{
    class WheelTorqueLimiter: IControlChainObj
    {
        private double TorqueLimit = 0;
        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            return (c == JoyControls.Throttle);
        }

        public double GetAxis(JoyControls c, double val)
        {
            if (c == JoyControls.Throttle)
                return val*TorqueLimit;
            else return val;
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

            var f = 1.0;
            if (Main.Drivetrain.GearRatios != null && data.Telemetry.Gear >= 1)
                f = Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1] / 5.5;
            var throttle = Math.Max(Main.GetAxisIn(JoyControls.Throttle), data.Telemetry.Throttle);
            TorqueLimit = 1;
            var NotGood = false;
            do
            {
                var wheelTorque = Main.Drivetrain.CalculateTorqueP(data.Telemetry.EngineRpm, TorqueLimit * throttle) * f;
                if (wheelTorque > 20000)
                {
                    TorqueLimit *= 0.999f;
                    NotGood = true;
                }
                else
                {
                    NotGood = false;
                }
                if (TorqueLimit <= 0.2f)
                {
                    TorqueLimit = 0.2f;
                    break;
                }
            } while (NotGood);

            TorqueLimit = 1.0f;
        }

        public bool Enabled { get; private set; }
        public bool Active { get; private set; }

        #endregion
    }
}