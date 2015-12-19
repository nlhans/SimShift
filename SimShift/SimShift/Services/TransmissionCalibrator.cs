using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    public enum TransmissionCalibratorStatus
    {
        Idle,
        FindThrottlePoint,
        FindClutchBitePoint,
        IterateCalibrationCycle
    }

    /// <summary>
    /// This module calibrates transmission whenever it can.
    /// </summary>
    public class TransmissionCalibrator : IControlChainObj
    {
        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }
        public bool Enabled { get { return true;  }}
        public bool Active { get; private set; }

        public TransmissionCalibratorStatus State { get { return this.state; }}

        private float calRpm = 0.0f;
        private int gearTest = 1;

        private bool rpmInRange = false;
        private DateTime rpmInRangeTimer = DateTime.Now;

        private TransmissionCalibratorStatus state = TransmissionCalibratorStatus.Idle;
        private DateTime stationary = DateTime.Now;
        private bool isStationary = false;

        public float err = 0.0f;

        private float clutch = 0.0f, throttle = 0.0f;
        public bool Requires(JoyControls c)
        {
            if (Active == false)
                return false;
            else
            {
                if (c == JoyControls.Clutch) return true;
                if (c == JoyControls.Throttle) return true;
                return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            if (!Active) return val;
            switch (c)  
            {
                case JoyControls.Throttle:
                    return throttle;
                case JoyControls.Clutch:
                    return clutch;
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

        public void TickTelemetry(IDataMiner data)
        {
            var rpm = data.Telemetry.EngineRpm;
            err = calRpm - rpm;
            if (err > 500)
                err = 500;
            if (err < -500)
                err = -500;

            switch (state)
            {
                case TransmissionCalibratorStatus.Idle:
                    Main.Transmission.OverruleShifts = false;

                    // If we're stationary for a few seconds, we can apply brakes and calibrate the clutch
                    bool wasStationary = isStationary;
                    isStationary = Math.Abs(data.Telemetry.Speed) < 1 && Main.GetAxisIn(JoyControls.Throttle) < 0.05 && data.Telemetry.EngineRpm > 200;

                    if (isStationary && !wasStationary)
                        stationary = DateTime.Now;

                    if (isStationary && DateTime.Now.Subtract(stationary).TotalMilliseconds > 2500)
                    {
                        clutch = 1.0f;

                        // CALIBRATE
                        state = TransmissionCalibratorStatus.IterateCalibrationCycle;
                    }
                    break;

                case TransmissionCalibratorStatus.FindThrottlePoint:

                    throttle += err/500000.0f;
                    if (throttle >= 0.8)
                    {
                        // Cannot rev the engine

                    }

                    var wasRpmInRange = rpmInRange;
                    rpmInRange = Math.Abs(err) < 5;

                    if (!wasRpmInRange && rpmInRange)
                        rpmInRangeTimer = DateTime.Now;

                    // stable at RPM
                    if (rpmInRange && DateTime.Now.Subtract(rpmInRangeTimer).TotalMilliseconds > 250)
                    {
                        // set error to 0
                        calRpm = rpm;
                        state = TransmissionCalibratorStatus.FindClutchBitePoint;
                    }

                    break;

                case TransmissionCalibratorStatus.FindClutchBitePoint:

                    if (Main.Transmission.IsShifting)
                    {
                        break;
                    }
                    if (data.Telemetry.Gear != gearTest)
                    {
                        Main.Transmission.Shift(data.Telemetry.Gear, gearTest, "normal");
                        break;
                    }

                    // Decrease clutch 0.25% at a time to find the bite point
                    clutch -= 0.25f/100.0f;

                    if (err > 50)
                    {
                        // We found the clutch bite point
                        var fs = File.AppendText("./gear-clutch");
                        fs.WriteLine(gearTest + "," + calRpm + "," + clutch);
                        fs.Close();
                        state = TransmissionCalibratorStatus.IterateCalibrationCycle;
                    }

                    break;

                case TransmissionCalibratorStatus.IterateCalibrationCycle:

                    clutch = 1;
                    gearTest ++;

                    calRpm = 700.0f;
                    // Find throttle point
                    state = TransmissionCalibratorStatus.FindThrottlePoint;
                    throttle = 0.001f;

                    rpmInRange = false;
                    
                    break;
            }

            //abort when we give power
            if (Main.GetAxisIn(JoyControls.Throttle) > 0.1)
            {
                stationary = DateTime.Now;
                state = TransmissionCalibratorStatus.Idle;
            }

            Active = state != TransmissionCalibratorStatus.Idle;

        }
    }
}
