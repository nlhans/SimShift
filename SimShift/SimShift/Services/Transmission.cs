using System;
using System.Collections.Generic;
using System.Diagnostics;
using SimShift.Data;
using SimShift.Dialogs;

namespace SimShift.Services
{
    public class Transmission : IControlChainObj
    {
        public int ShiftFrame = 0;
        public List<ShiftPatternFrame> ShiftPattern; 
        public Dictionary<string, ShifterTableConfiguration> Configurations = new Dictionary<string, ShifterTableConfiguration>();
        public string Active { get; private set; }

        public int GameGear { get; private set; }
        public int ShifterGear { get { return ShifterNewGear; } }
        public static bool IsShifting { get; private set; }

        public int ShiftCtrlOldGear { get; private set; }
        public int ShiftCtrlNewGear { get; private set; }
        public int ShiftCtrlOldRange { get; private set; }
        public int ShiftCtrlNewRange { get; private set; }

        public int ShifterOldGear { get { return ShiftCtrlOldGear + ShiftCtrlOldRange * 6; } }
        public int ShifterNewGear { get { return ShiftCtrlNewGear + ShiftCtrlNewRange * 6; } }

        public DateTime TransmissionFreezeUntill { get; private set; }
        public bool TransmissionFrozen { get { return TransmissionFreezeUntill > DateTime.Now; } }

        public DateTime ChangeModeFrozenUntill { get; private set; }
        public bool ChangeModeFrozen { get { return ChangeModeFrozenUntill > DateTime.Now; } }

        public DateTime RangeButtonFreeze1Untill { get; private set; }
        public DateTime RangeButtonFreeze2Untill { get; private set; }
        public int RangeButtonSelectPhase
        {
            get
            {
                if (RangeButtonFreeze1Untill > DateTime.Now) return 1; // phase 1
                if (RangeButtonFreeze2Untill > DateTime.Now) return 2; // phase 2
                return 0; // phase 0
            }
        }

        public bool _tempProfilesForTrailer { get; private set; }
        public bool DrivingInReverse { get; private set; }

        public Transmission()
        {
            _tempProfilesForTrailer = true;
            // Stock: Add 3 profiles
            Configurations.Add("Economy", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Economy, 3));
            Configurations.Add("Efficiency", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Efficiency, 5));
            Configurations.Add("Performance", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Performance, 7));
            Configurations.Add("PeakRpm", new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, 5));

            LoadShiftPattern("Normal");
            SetActiveConfiguration("Performance");

            // Initialize all shfiting stuff.
            Shift(0,1);
            IsShifting = false;
        }

        public void SetActiveConfiguration(string ac)
        {
            if (Configurations.ContainsKey(ac))
            {
                Active = ac;
            }
        }

        #region Transmission Shift logics
        public void Shift(int fromGear, int toGear)
        {
            // Copy old control to new control values
            ShiftCtrlOldGear = fromGear;
            if (ShiftCtrlOldGear == -1) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear == 0) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear >= 1 && ShiftCtrlOldGear <= 6) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear >= 7 && ShiftCtrlOldGear <= 12) ShiftCtrlOldRange = 1;
            else if (ShiftCtrlOldGear >= 13 && ShiftCtrlOldGear <= 18) ShiftCtrlOldRange = 2;

            // Determine new range
            if (toGear == -1)
            {
                ShiftCtrlNewGear = -1;
                ShiftCtrlNewRange = 0;
            }
            else if (toGear == 0)
            {
                ShiftCtrlNewGear = 0;
                ShiftCtrlNewRange = 0;
            }
            else if (toGear >= 1 && toGear <= 6)
            {
                ShiftCtrlNewGear = toGear;
                ShiftCtrlNewRange = 0;
            }
            else if (toGear >= 7 && toGear <= 12)
            {
                ShiftCtrlNewGear = toGear - 6;
                ShiftCtrlNewRange = 1;
            }
            else if (toGear >= 13 && toGear <= 18)
            {
                ShiftCtrlNewGear = toGear - 12;
                ShiftCtrlNewRange = 2;
            }

            ShiftFrame = 0;
            IsShifting = true;

        }


        private bool IsGearInRange(int gear, int range)
        {
            if (range == 0)
            {
                if (gear >= -1 && gear <= 6) return true;
            }
            if (range == 1)
            {
                if (gear >= 7 && gear <= 12) return true;
            }
            if (range == 2)
            {
                if (gear >= 13 && gear <= 18) return true;
            }
            return false;
        }

        private void LoadShiftPattern(string pattern)
        {
            // TODO: Patterns are not loaded from files yet.
            ShiftPattern = new List<ShiftPatternFrame>();

            // Phase 1: engage clutch
            ShiftPattern.Add(new ShiftPatternFrame(0, 0, true, false));
            //ShiftPattern.Add(new ShiftPatternFrame(0, 0.7, true, false));
            //ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.4, true, false));
            //ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, true, false));

            // Phase 2: disengage old gear
            ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
            ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));

            // Phase 3: engage new gear
            //ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
            //ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
            
            // Phase 4: disengage clutch
            //ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, false, true));
            //ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.4, false, true));
            //ShiftPattern.Add(new ShiftPatternFrame(0.0, 0.7, false, true));
            ShiftPattern.Add(new ShiftPatternFrame(0.0, 0, false, true));
        }
        #endregion

        #region Transmission telemetry logic
        public void TickTelemetry(Ets2DataMiner data)
        {
            if(_tempProfilesForTrailer && !data.Telemetry.trailer_attached)
            {
                Configurations.Clear();

                _tempProfilesForTrailer = false;
                // Stock: Add 3 profiles
                Configurations.Add("Economy", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Economy, 10));
                Configurations.Add("Efficiency", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Efficiency, 15));
                Configurations.Add("Performance", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Performance, 20));
                Configurations.Add("PeakRpm", new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, 5));

            }
            else if (!_tempProfilesForTrailer && data.Telemetry.trailer_attached)
            {
                Configurations.Clear();

                _tempProfilesForTrailer = true;
                // Stock: Add 3 profiles
                Configurations.Add("Economy", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Economy, 3));
                Configurations.Add("Efficiency", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Efficiency, 5));
                Configurations.Add("Performance", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Performance, 7));
                Configurations.Add("PeakRpm", new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, 5));
            }

            // TODO: Add generic telemetry object
            if (IsShifting) return;
            if (TransmissionFrozen) return;


            var lookupResult = Configurations[Active].Lookup(data.Telemetry.speed*3.6, Main.GetAxisIn(JoyControls.Throttle));
            var idealGear = lookupResult.Gear;

            GameGear = data.Telemetry.gear;
            if (data.Telemetry.gear == 0 && ShiftCtrlNewGear != 0)
            {
                Debug.WriteLine("Timeout");
                ShiftCtrlNewGear = 0;
                TransmissionFreezeUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 250));
                return;
            }

            if (DrivingInReverse)
            {
                if (GameGear != -1)
                {
                    Debug.WriteLine("Shift from " + data.Telemetry.gear + " to  " + idealGear);
                    Shift(data.Telemetry.gear, -1);
                }
                return;
            }

            if (idealGear != data.Telemetry.gear)
            {
                Debug.WriteLine("Shift from " + data.Telemetry.gear + " to  " + idealGear);
                Shift(data.Telemetry.gear, idealGear);
            }
        }
        #endregion

        #region Control Chain Methods
        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                // Only required when changing
                case JoyControls.Throttle:
                case JoyControls.Clutch:
                    return IsShifting;

                // All gears.
                case JoyControls.GearR:
                case JoyControls.Gear1:
                case JoyControls.Gear2:
                case JoyControls.Gear3:
                case JoyControls.Gear4:
                case JoyControls.Gear5:
                case JoyControls.Gear6:
                case JoyControls.GearRange2:
                case JoyControls.GearRange1:
                    return true;

                case JoyControls.GearUp:
                case JoyControls.GearDown:
                    return true;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            if (ShiftFrame >= ShiftPattern.Count)
                return val;
            switch (c)
            {
                case JoyControls.Throttle:
                    return ShiftPattern[ShiftFrame].Throttle * val;

                case JoyControls.Clutch:
                    return ShiftPattern[ShiftFrame].Clutch;

                default:
                    return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch (c)
            {
                case JoyControls.Gear1:
                    return GetShiftButton(1);
                case JoyControls.Gear2:
                    return GetShiftButton(2);
                case JoyControls.Gear3:
                    return GetShiftButton(3);
                case JoyControls.Gear4:
                    return GetShiftButton(4);
                case JoyControls.Gear5:
                    return GetShiftButton(5);
                case JoyControls.Gear6:
                    return GetShiftButton(6);
                case JoyControls.GearR:
                    return GetShiftButton(-1);
                case JoyControls.GearRange1:
                    return GetRangeButton(1);
                case JoyControls.GearRange2:
                    return GetRangeButton(2);

                case JoyControls.GearUp:
                    if(val)
                    {
                        if(!DrivingInReverse && !ChangeModeFrozen)
                        {
                            // We're already going forwards.
                            // Change shifter profile
                            switch(Active)
                            {
                                case "Performance":
                                    SetActiveConfiguration("Economy");
                                    break;

                                case "Efficiency":
                                    SetActiveConfiguration("Performance");
                                    break;

                                case "Economy":
                                    SetActiveConfiguration("Efficiency");
                                    break;
                            }
                            ChangeModeFrozenUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 1000));
                        }
                        DrivingInReverse = false;
                    }
                    return false;

                case JoyControls.GearDown:
                    if (val)
                        DrivingInReverse = true;
                    return false;

                default:
                    return val;
            }
        }

        private bool GetRangeButton(int r)
        {
            if (IsShifting && ShiftCtrlNewRange != ShiftCtrlOldRange)
            {
                switch (RangeButtonSelectPhase)
                {
                        // On
                    case 1:
                        if (r == 2) return false;
                        if (ShifterOldGear < 7 && ShifterNewGear >= 7) return true;
                        if (ShifterOldGear >= 7 && ShifterNewGear < 7) return true;
                        return false;

                        // Off
                    case 2:
                        return false;

                        // Evaluate and set phase 1(on) / phase 2 (off) timings
                    default:
                        RangeButtonFreeze1Untill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 100)); //150ms ON
                        RangeButtonFreeze2Untill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 1500)); //150ms OFF
                        if (r == 2) return false;
                        if (ShifterOldGear >= 7 && ShifterNewGear >= 7) return false;
                        if (ShifterOldGear < 7 && ShifterNewGear < 7) return false;

                        return true;
                }
            }

            return false;
        }


        private bool GetShiftButton(int b)
        {
            if (IsShifting)
            {
                if (ShiftFrame >= ShiftPattern.Count) return false;
                if (ShiftPattern[ShiftFrame].UseOldGear) return (b == ShiftCtrlOldGear);
                if (ShiftPattern[ShiftFrame].UseNewGear) return (b == ShiftCtrlNewGear);
                return false;
            }

            return (b == ShiftCtrlNewGear);
        }

        private int subTicker = 0;
        public void TickControls()
        {
            if (IsShifting)
            {
                subTicker++;
                if (subTicker >  3) subTicker = 0;

                if (false || subTicker == 0)
                {
                    ShiftFrame++;
                    if (ShiftFrame >= ShiftPattern.Count)
                    {
                        IsShifting = false;
                        TransmissionFreezeUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 200+ShifterNewGear*50));
                    }
                }
            }
        }
        #endregion

        }
}