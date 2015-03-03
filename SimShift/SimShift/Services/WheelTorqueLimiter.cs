using System;
using System.Collections.Generic;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    /// This module is a gimmick to simulate a torque limiter for the drive wheels. It practically limits the throttle in lower gears, because it may exceed torque limit of the drivetrain.
    /// /
    /// </summary>
    class WheelTorqueLimiter: IControlChainObj
    {
        public bool Enabled { get; private set; }
        public bool Active { get; private set; }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        //
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

        #endregion
    }
}