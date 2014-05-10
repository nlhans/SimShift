using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public enum DrivetrainCalibrationStage
    {
        None,
        StartIdleRpm,
        FinishIdleRpm,
        StartMaxRpm,
        FinishMaxRpm,
        StartGears,
        FinishGears,
        StartGearRatios,
        //..
        EndGearRatios,
        ShiftToFirst
    }

    public class DrivetrainCalibrator : IControlChainObj
    {
        public bool Calibrating { get; private set; }

        public DateTime MeasurementSettletime { get; private set; }

        public bool MeasurementSettled
        {
            get { return DateTime.Now > MeasurementSettletime; }
        }

        private bool reqThrottle;
        private bool reqClutch;
        private bool reqGears { get { return Main.Transmission.OverruleShifts; } set { Main.Transmission.OverruleShifts = value; } }

        private double throttle;
        private double clutch;
        private int gear;

        private DrivetrainCalibrationStage stage;
        private DrivetrainCalibrationStage nextStage;

        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    return reqThrottle;
                case JoyControls.Clutch:
                    return reqClutch;

                case JoyControls.Gear1:
                case JoyControls.Gear2:
                case JoyControls.Gear3:
                case JoyControls.Gear4:
                case JoyControls.Gear5:
                case JoyControls.Gear6:
                case JoyControls.Gear7:
                case JoyControls.Gear8:
                case JoyControls.GearR:
                    return false;//return reqGears;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch(c)
            {
                case JoyControls.Throttle:
                    return throttle;
                    break;

                case JoyControls.Clutch:
                    return clutch;
                    break;

                default:
                    return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch(c)
            {
                case JoyControls.Gear1:
                    return 1 == gear;
                case JoyControls.Gear2:
                    return 2 == gear;
                case JoyControls.Gear3:
                    return 3 == gear;
                case JoyControls.Gear4:
                    return 4 == gear;
                case JoyControls.Gear5:
                    return 5 == gear;
                case JoyControls.Gear6:
                    return 6 == gear;
                case JoyControls.Gear7:
                    return 7 == gear;
                case JoyControls.Gear8:
                    return 8 == gear;
                case JoyControls.GearR:
                    return -1 == gear;
                default:
                    return val;
            }
        }

        public void TickControls()
        {
            //
        }

        private double previousEngineRpm = 0;
        private double previousThrottle = 0;
        private double maxRpmTarget = 0;
        private double maxRpmMeasured = 0;
        private bool calibrationPreDone = false;
        private string calibrateShiftStyle = "up_1thr";
        private int samplesTaken = 0;
        private double sample = 0;

        private int shiftToFirstRangeAttempt = 0;

        private float gearRatioSpeedCruise = 0.0f;

        public void TickTelemetry(IDataMiner data)
        {
            bool wasCalibrating = Calibrating;
            Calibrating = !Main.Drivetrain.Calibrated;
            if(!wasCalibrating && Calibrating)
            {
                Debug.WriteLine("now calibrating");
                stage = DrivetrainCalibrationStage.StartIdleRpm;
            }

            if(stage!=DrivetrainCalibrationStage.None && !Calibrating)
                stage = DrivetrainCalibrationStage.None;
            ;
            switch(stage)
            {
                case DrivetrainCalibrationStage.None :
                    reqGears = false;
                    reqThrottle = false;
                    reqClutch = false;
                    break;

                case DrivetrainCalibrationStage.StartIdleRpm:
                    
                    reqClutch = true;
                    reqThrottle = true;
                    reqGears = true;
                    Main.Transmission.Shift(data.Telemetry.Gear, 0, calibrateShiftStyle);
                    if (data.Telemetry.EngineRpm < 300)
                    {
                        throttle = 1;
                        clutch = 1;
                        gear = 0;
                    }
                    else if (data.Telemetry.EngineRpm>2000)
                    {
                        throttle = 0;
                        clutch = 1;
                        gear = 0;
                    }else
                    {
                        throttle = 0;
                        clutch = 1;
                        gear = 0;

                        if (Math.Abs(data.Telemetry.EngineRpm - previousEngineRpm) < 1){
                            stage = DrivetrainCalibrationStage.FinishIdleRpm;

                            MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 2750));
                        }
                    }
                        previousEngineRpm = data.Telemetry.EngineRpm;
                    break;
                case DrivetrainCalibrationStage.FinishIdleRpm:
                    if (MeasurementSettled)
                    {
                        Debug.WriteLine("Idle RPM: " + data.Telemetry.EngineRpm);
                        if (data.Telemetry.EngineRpm < 300)
                        {
                            stage = DrivetrainCalibrationStage.StartIdleRpm;
                        }
                        else
                        {
                            Main.Drivetrain.StallRpm = data.Telemetry.EngineRpm;

                            stage = DrivetrainCalibrationStage.StartMaxRpm;
                            maxRpmTarget = data.Telemetry.EngineRpm + 1000;
                            previousThrottle = 0;
                        }
                    }
                    break;

                case DrivetrainCalibrationStage.StartMaxRpm:
                    reqClutch = true;
                    reqThrottle = true;
                    reqGears = true;
                    
                    clutch = 1;
                    throttle = 1;
                    maxRpmMeasured = 0;

                    MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0,1500));
                    stage = DrivetrainCalibrationStage.FinishMaxRpm;

                    break;
                case DrivetrainCalibrationStage.FinishMaxRpm:

                    throttle = 1;
                    maxRpmMeasured = Math.Max(maxRpmMeasured, data.Telemetry.EngineRpm);

                    if (MeasurementSettled)
                    {
                        if (Math.Abs(maxRpmMeasured-Main.Drivetrain.StallRpm) < 500)
                        {
                            Debug.WriteLine("Totally messed up MAX RPM.. resetting");
                            stage = DrivetrainCalibrationStage.StartIdleRpm;
                        }
                        else
                        {
                            Debug.WriteLine("Max RPM approx: " + maxRpmMeasured);

                            Main.Drivetrain.MaximumRpm = maxRpmMeasured-300;


                            stage = DrivetrainCalibrationStage.ShiftToFirst;
                            nextStage = DrivetrainCalibrationStage.StartGears;
                        }
                    }
                    break;

                case DrivetrainCalibrationStage.StartGears:
                    reqClutch = true;
                    reqThrottle = true;
                    reqGears = true;

                    throttle = 0;
                    clutch = 1;
                    gear++;
                    Main.Transmission.Shift(data.Telemetry.Gear, gear, calibrateShiftStyle);
                    MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0,500));

                    stage = DrivetrainCalibrationStage.FinishGears;

                    break;
                case DrivetrainCalibrationStage.FinishGears:

                    if (MeasurementSettled &&  !Transmission.IsShifting)
                    {
                        if(data.Telemetry.Gear != gear)
                        {
                            gear--;
                            // Car doesn't have this gear.
                            Debug.WriteLine("Gears: " + gear);

                            if (gear <= 0)
                            {
                                Debug.WriteLine("That's not right");
                                stage = DrivetrainCalibrationStage.StartGears;

                            }
                            else
                            {
                                Main.Drivetrain.Gears = gear;
                                Main.Drivetrain.GearRatios = new double[gear];

                                stage = DrivetrainCalibrationStage.ShiftToFirst;
                                nextStage = DrivetrainCalibrationStage.StartGearRatios;
                                MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 500));
                                calibrationPreDone = false;
                            }
                            gear = 0;
                        }
                        else
                        {
                            stage=DrivetrainCalibrationStage.StartGears;
                        }
                    }
                    break;

                case DrivetrainCalibrationStage.ShiftToFirst:
                    if (!Transmission.IsShifting && MeasurementSettled)
                    {
                        if (data.Telemetry.Gear != 1)
                        {
                            Main.Transmission.Shift(shiftToFirstRangeAttempt*Main.Transmission.RangeSize + 1, 1,
                                                    calibrateShiftStyle);
                            shiftToFirstRangeAttempt++;

                            MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 100));
                            if (shiftToFirstRangeAttempt > 3) shiftToFirstRangeAttempt = 0;
                        }
                        else
                        {
                            stage = nextStage;
                            MeasurementSettletime = DateTime.MaxValue;
                        }
                    }


                    break;


                case DrivetrainCalibrationStage.EndGearRatios:

                    if (Main.Drivetrain.GearRatios.Length >= data.Telemetry.Gear)
                    {
                        if(data.Telemetry.Gear<=0)
                        {
                            stage = DrivetrainCalibrationStage.StartGearRatios;
                            break;
                        }
                        if (data.Telemetry.EngineRpm > Main.Drivetrain.StallRpm*1.15)
                        {
                            var gr = Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1];
                            if (gr != 0)
                            {
                                stage = DrivetrainCalibrationStage.StartGearRatios;
                                break;
                            }
                            reqThrottle = true;
                            throttle = gearRatioSpeedCruise - data.Telemetry.Speed;

                            var ratio = data.Telemetry.EngineRpm / (3.6 * data.Telemetry.Speed);
                            if (ratio > 400 || ratio < 1)
                            {
                                stage = DrivetrainCalibrationStage.StartGearRatios;
                                break;
                            }

                            Debug.WriteLine("Gear " + data.Telemetry.Gear + " : " + ratio);

                            // start sampling
                            if (sample == 0) sample = ratio;
                            else sample = sample * 0.9 + ratio * 0.1;
                            samplesTaken ++;

                            if(samplesTaken==50)
                            {
                                Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1] = sample;
                            }
                        }
                        else
                        {
                            stage = DrivetrainCalibrationStage.StartGearRatios;
                            break;
                        }
                    }
                    else
                    {
                        stage = DrivetrainCalibrationStage.StartGearRatios;
                    }
                    break;

                case DrivetrainCalibrationStage.StartGearRatios:
                    
                    reqGears = false;
                    reqThrottle = false;
                    reqClutch = false;

                    // Activate get-home-mode; which shifts at 4x stall rpm
                    Main.Transmission.GetHomeMode = true;

                    if (data.Telemetry.EngineRpm > Main.Drivetrain.StallRpm*2)
                    {
                        // Driving at reasonable rpm's.

                        if (data.Telemetry.Gear > 0)
                        {
                            if (Main.Drivetrain.GearRatios.Length >= data.Telemetry.Gear &&
                                data.Telemetry.EngineRpm > Main.Drivetrain.StallRpm * 2)
                            {
                                var gr = Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1];


                                if (gr == 0)
                                {
                                    samplesTaken = 0;
                                    gearRatioSpeedCruise = data.Telemetry.Speed;
                                    stage = DrivetrainCalibrationStage.EndGearRatios;
                                }
                            }
                        }

                        var GearsCalibrated = true;
                        for (int i = 0; i < Main.Drivetrain.Gears; i++)
                            if (Main.Drivetrain.GearRatios[i] < 1)
                                GearsCalibrated = false;

                        if (GearsCalibrated)
                        {
                            if (MeasurementSettled)
                            {
                                Main.Transmission.GetHomeMode = false;
                                Debug.WriteLine("Calibration done");
                                stage = DrivetrainCalibrationStage.None;

                                Main.Store(Main.Drivetrain.ExportParameters(), Main.Drivetrain.File);
                                Main.Load(Main.Drivetrain, Main.Drivetrain.File);
                            }

                            if (!calibrationPreDone)
                            {
                                calibrationPreDone = true;
                                MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 3));
                            }
                        }

                    }

                    break;
            }
        }

        #endregion
    }
}
