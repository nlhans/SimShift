using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    public enum PowerMeterState
    {
        Idle,
        Prearm,
        Revup,
        Revdown,
        Cooldown
    }

    /// <summary>
    ///  Estimates power level of engine by revving up
    /// </summary>
    class Ets2PowerMeter : IControlChainObj
    {
        public PowerMeterState State;
        public IEnumerable<string> SimulatorsOnly {
            get { return new string[0]; }
        }
        public IEnumerable<string> SimulatorsBan
        {
            get { return new string[0]; }
        }
        public bool Enabled { get; private set; }
        public bool Active { get; private set; }
        public bool Requires(JoyControls c)
        {
            return (c == JoyControls.Clutch || c == JoyControls.Throttle);
        }

        private float preArmThr;

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (!Active)
                        return val;
                    else if (State == PowerMeterState.Prearm)
                        return preArmThr;
                    else if (State == PowerMeterState.Revup)
                        return 1;
                    else if (State == PowerMeterState.Revdown)
                        return 0;
                    else return val;
                    break;
                case JoyControls.Clutch:
                    return Active ? 1 : val;

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
        }

        private DateTime startRevup;
        private DateTime endRevup;

        private DateTime startRevdown;
        private DateTime endRevdown;

        private float integralRevver = 0.0f;
        private float prearmSettler = 0;
        private float revdownRpm;

        public void TickTelemetry(IDataMiner data)
        {
            Enabled = true;
            switch (State)
            {
                case PowerMeterState.Idle:
                    if (Main.GetButtonIn(JoyControls.MeasurePower) && false)
                    {
                        integralRevver = 0;
                        State = PowerMeterState.Prearm;
                        this.Active = true;
                    }
                    else
                    {
                        this.Active = false;
                    }
                    break;

                    case PowerMeterState.Prearm:
                    preArmThr =  (1000 - data.Telemetry.EngineRpm)/1500;
                    if (Math.Abs(data.Telemetry.EngineRpm - 1000) < 100)
                    {
                        integralRevver += (1000 - data.Telemetry.EngineRpm)/750000.0f;
                    }
                    else
                    {
                        integralRevver = 0;
                    }
                    preArmThr += integralRevver;

                    if (preArmThr > 0.7f) preArmThr = 0.7f;
                    if (preArmThr < 0) preArmThr = 0;

                    if (Math.Abs(data.Telemetry.EngineRpm - 1000) < 5)
                    {
                        prearmSettler++;
                        if (prearmSettler > 200)
                        {
                            startRevup = DateTime.Now;
                            State = PowerMeterState.Revup;
                        }
                    }
                    else
                    {
                        prearmSettler = 0;
                    }
                    break;

                    case PowerMeterState.Revup:
                    if (data.Telemetry.EngineRpm >= 2000)
                    {
                        endRevup = DateTime.Now;
                        startRevdown = DateTime.Now;
                        State = PowerMeterState.Revdown;
                        revdownRpm = data.Telemetry.EngineRpm;
                    }
                    break;

                case PowerMeterState.Revdown:
                    if (data.Telemetry.EngineRpm <=1000)
                    {
                        endRevdown = DateTime.Now;
                        State = PowerMeterState.Cooldown;
                        var fallTime = endRevdown.Subtract(startRevdown).TotalMilliseconds/1000.0;
                        var fallRpm = revdownRpm - data.Telemetry.EngineRpm;
                        Console.WriteLine("Rev up: " + (endRevup.Subtract(startRevup).TotalMilliseconds) + "ms, rev down: " + (fallTime) + "ms (" + (fallRpm/fallTime)+"rpm/s");
                    }
                    break;

                case PowerMeterState.Cooldown:
                    Active = false;
                    if (data.Telemetry.EngineRpm < 700)
                    {
                        State = PowerMeterState.Idle;
                    }
                    break;

            }
        }
    }
}
