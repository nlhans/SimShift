using System;
using System.Collections.Generic;
using System.Diagnostics;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Dialogs;

namespace SimShift.Services
{
    public class Transmission : IControlChainObj
    {
        public int RangeSize = 6;

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

        public int ShifterOldGear { get { return ShiftCtrlOldGear + ShiftCtrlOldRange * RangeSize; } }
        public int ShifterNewGear { get { return ShiftCtrlNewGear + ShiftCtrlNewRange * RangeSize; } }

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

        private double transmissionThrottle;

        public Transmission()
        {
            _tempProfilesForTrailer = true;
            // Stock: Add 3 profiles
            Configurations.Add("Economy", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Economy, 9));
            Configurations.Add("Efficiency", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Efficiency, 9));
            Configurations.Add("Performance", new ShifterTableConfiguration(ShifterTableConfigurationDefault.Performance, 9));
            Configurations.Add("PeakRpm", new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, 9));
            Configurations.Add("Opa", new ShifterTableConfiguration(ShifterTableConfigurationDefault.AlsEenOpa, 13));

            LoadShiftPattern("Normal");
            SetActiveConfiguration("Performance");

            // Initialize all shfiting stuff.
            Shift(0,1,"up_1thr");
            IsShifting = false;
        }

        public ShifterTableConfiguration GetActiveConfiguration()
        {
            if (Configurations.ContainsKey(Active) == false)
                return default(ShifterTableConfiguration);
            return Configurations[Active];
        }

        public void SetActiveConfiguration(string ac)
        {
            if (Configurations.ContainsKey(ac))
            {
                Active = ac;
            }
        }

        #region Transmission Shift logics
        public void Shift(int fromGear, int toGear, string style)
        {
            LoadShiftPattern(style);

            // Copy old control to new control values
            ShiftCtrlOldGear = fromGear;
            if (ShiftCtrlOldGear == -1) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear == 0) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear >= 1 && ShiftCtrlOldGear <= RangeSize) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear >= RangeSize + 1 && ShiftCtrlOldGear <= 2 * RangeSize) ShiftCtrlOldRange = 1;
            else if (ShiftCtrlOldGear >= 2 * RangeSize + 1 && ShiftCtrlOldGear <= 3 * RangeSize) ShiftCtrlOldRange = 2;
            ShiftCtrlOldGear -= ShiftCtrlOldRange * RangeSize;

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
            else if (toGear >= 1 && toGear <= RangeSize)
            {
                ShiftCtrlNewGear = toGear;
                ShiftCtrlNewRange = 0;
            }
            else if (toGear >= RangeSize+1 && toGear <= RangeSize * 2)
            {
                ShiftCtrlNewGear = toGear - RangeSize;
                ShiftCtrlNewRange = 1;
            }
            else if (toGear >= RangeSize*2+1 && toGear <= RangeSize * 3)
            {
                ShiftCtrlNewGear = toGear - RangeSize*2;
                ShiftCtrlNewRange = 2;
            }

            ShiftFrame = 0;
            IsShifting = true;

        }

        private void LoadShiftPattern(string pattern)
        {
            var engageLength = 6.0f;
            var disengageLength = 6.0f;
            switch(pattern)
            {
                // very slow
                case "up_0thr":
                    // TODO: Patterns are not loaded from files yet.
                    ShiftPattern = new List<ShiftPatternFrame>();

                    // Phase 1: engage clutch
                    /*
                    ShiftPattern.Add(new ShiftPatternFrame(0, 1, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0, 0.7, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.4, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, true, false));*/
                    for (int i = 0; i < engageLength; i++)
                        ShiftPattern.Add(new ShiftPatternFrame(1.0/engageLength, 1 - 1.0/engageLength, true, false));

                        // Phase 2: disengage old gear
                        ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));

                    // Phase 3: engage new gear
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));

                    // Phase 4: disengage clutch
                    /*
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.4, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0, 0.7, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0,1, false, true));*/
                    for (int i = 0; i < disengageLength; i++)
                        ShiftPattern.Add(new ShiftPatternFrame(1 - 1.0 / engageLength, 1.0 / engageLength, false, true));
                    break;

                case "up_1thr":
                    // TODO: Patterns are not loaded from files yet.
                    ShiftPattern = new List<ShiftPatternFrame>();

                    // Phase 1: engage clutch
                    ShiftPattern.Add(new ShiftPatternFrame(0, 0.6, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0, 0.3, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, true, false));

                    // Phase 2: disengage old gear
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));

                    // Phase 3: engage new gear
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));

                    // Phase 4: disengage clutch
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0, 0.3, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0, 0.6, false, true));
                    break;

                case "down_0thr":
                    // TODO: Patterns are not loaded from files yet.
                    ShiftPattern = new List<ShiftPatternFrame>();

                    // Phase 1: engage clutch
                    ShiftPattern.Add(new ShiftPatternFrame(0, 1, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0, 0.7, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.4, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, true, false));

                    // Phase 2: disengage old gear
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 1, true, false, false));

                    // Phase 3: engage new gear
                    ShiftPattern.Add(new ShiftPatternFrame(1, 1, true, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 1, true, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0, false, true));

                    // Phase 4: disengage clutch
                    for (int i = 0; i < 25; i++)
                        ShiftPattern.Add(new ShiftPatternFrame((25 - i) / 25.0, i / 25.0, false, true));
                    break;

                case "down_1thr":
                    // TODO: Patterns are not loaded from files yet.
                    ShiftPattern = new List<ShiftPatternFrame>();

                    // Phase 1: engage clutch
                    ShiftPattern.Add(new ShiftPatternFrame(0, 1, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0, 0.7, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.2, 0.4, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.2, true, false));
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0.1, true, false));

                    // Phase 2: disengage old gear
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0.0, false, false));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0.0, false, false));

                    // Phase 3: engage new gear
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0.0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(1, 0.0, false, true));

                    // Phase 4: disengage clutch
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0.1, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.2, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.2, 0.4, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0, 0.7, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0, 1, false, true));
                    break;
            }
        }
        #endregion

        #region Transmission telemetry logic
        public void TickTelemetry(IDataMiner data)
        {
            if (data.TransmissionSupportsRanges)
                RangeSize = 6;
            else
                RangeSize = 8;

            // TODO: Add generic telemetry object
            GameGear = data.Telemetry.Gear;
            if (IsShifting) return;
            if (TransmissionFrozen) return;
            if (Configurations.ContainsKey(Active) == false) return;
            shiftRetry = 0;
            
            var lookupResult = Configurations[Active].Lookup(data.Telemetry.Speed*3.6, transmissionThrottle);
            var idealGear = lookupResult.Gear;

            if (data.Telemetry.Gear == 0 && ShiftCtrlNewGear != 0)
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
                    Debug.WriteLine("Shift from " + data.Telemetry.Gear + " to  " + idealGear);
                    Shift(data.Telemetry.Gear, -1, "up_1thr");
                }
                return;
            }

            if (idealGear != data.Telemetry.Gear)
            {
                var upShift = idealGear > data.Telemetry.Gear;
                var fullThrottle = data.Telemetry.Throttle > 0.6;

                var shiftStyle =( upShift ? "up" : "down") + "_" + (fullThrottle ? "1" : "0") + "thr";

                Debug.WriteLine("Shift from " + data.Telemetry.Gear + " to  " + idealGear);
                Shift(data.Telemetry.Gear, idealGear, shiftStyle);
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
                    return true;

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
                    return true;

                case JoyControls.GearRange2:
                case JoyControls.GearRange1:
                    return Main.Data.Active.TransmissionSupportsRanges;

                case JoyControls.Gear7:
                case JoyControls.Gear8:
                    return !Main.Data.Active.TransmissionSupportsRanges;

                case JoyControls.GearUp:
                case JoyControls.GearDown:
                    return true;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    transmissionThrottle = val > 0 && val < 1 ? val : 0;

                    if (ShiftFrame >= ShiftPattern.Count)
                        return val;
                    return IsShifting ? ShiftPattern[ShiftFrame].Throttle*val : val;

                case JoyControls.Clutch:
                    if (ShiftFrame >= ShiftPattern.Count)
                        return val;
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
                case JoyControls.Gear7:
                    return GetShiftButton(7);
                case JoyControls.Gear8:
                    return GetShiftButton(8);
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
                                case "Opa":
                                    SetActiveConfiguration("Economy");
                                    break;

                                case "Economy":
                                    SetActiveConfiguration("Efficiency");
                                    break;

                                case "Efficiency":
                                    SetActiveConfiguration("Performance");
                                    break;

                                case "Performance":
                                    SetActiveConfiguration("PeakRpm");
                                    break;

                                case "PeakRpm":
                                    SetActiveConfiguration("Opa");
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

        /*

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
         * */

        private bool GetRangeButton(int r)
        {
            if (Main.Data.Active == null || !Main.Data.Active.TransmissionSupportsRanges) return false;
            if (IsShifting && ShiftCtrlNewRange != ShiftCtrlOldRange)
            {
                // Going to range 1 when old gear was outside range 1,
                // and new is in range 1.
                var engagingToRange1 = ShifterOldGear >= 7 && ShifterNewGear < 7;

                // Range 2 is engaged when the old gear was range 1 or 3, and the new one is range 2.
                var engagingToRange2 = (ShifterOldGear < 7 || ShifterOldGear > 12) &&
                    ShifterNewGear >= 7 && ShifterNewGear <= 12;

                // Range 2 is engaged when the old gear was range 1 or 2, and the new one is range 3.
                var engagingToRange3 = ShifterOldGear < 13 &&
                                       ShifterNewGear >= 13;

                var engageR1Status = false;
                var engageR2Status = false;

                if (ShiftCtrlOldRange == 0)
                {    if (ShiftCtrlNewRange == 1)
                        engageR1Status = true;
                    else engageR2Status = true;
                }
                else if (ShiftCtrlOldRange == 1)
                {
                    if (ShiftCtrlNewRange == 0)
                        engageR1Status = true;
                    else
                    {
                        engageR1Status = true;
                        engageR2Status = true;
                    }
                }
                else if (ShiftCtrlOldRange == 2)
                {
                    if (ShiftCtrlNewRange == 0)
                        engageR2Status = true;
                    else
                    {
                        engageR1Status = true;
                        engageR2Status = true;
                    }

                }


                switch (RangeButtonSelectPhase)
                {
                        // On
                    case 1:
                        if (r == 1) return engageR1Status;
                        if(r==2) return engageR2Status;

                        return false;

                        // Off
                    case 2:
                        return false;

                        // Evaluate and set phase 1(on) / phase 2 (off) timings
                    default:

                        Debug.WriteLine("Shift " + ShifterOldGear + "(" + ShiftCtrlOldRange + ") to " + ShifterNewGear + "(" + ShiftCtrlNewRange+ ")");
                        Debug.WriteLine("R1: " + engageR1Status + " / R2: " +engageR2Status);
                        if (r == 1 && !engageR1Status) return false;
                        if (r == 2 && !engageR2Status) return false;

                        RangeButtonFreeze1Untill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 100)); //150ms ON
                        RangeButtonFreeze2Untill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 1500)); //150ms OFF

                        Debug.WriteLine("Yes");
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

        private int shiftRetry = 0;
        public void TickControls()
        {
            if (IsShifting)
            {

                ShiftFrame++;
                if (ShiftFrame > ShiftPattern.Count)
                    ShiftFrame = 0;
                if (shiftRetry < 10 && ShiftFrame > 4 && ShiftPattern[ShiftFrame - 3].UseNewGear &&
                    GameGear != ShifterNewGear)
                {
                    // So we are shifting, check lagging by 1, and new gear doesn't work
                    // We re-shfit
                    var tmp = ShiftFrame;
                    Shift(GameGear, ShifterNewGear, "up_1thr");
                    ShiftFrame = tmp - 4;
                    shiftRetry++;

                    if (ShiftCtrlNewRange != ShiftCtrlOldRange)
                    {

                        ShiftFrame = 0;

                        RangeButtonFreeze1Untill = DateTime.Now;
                        RangeButtonFreeze2Untill = DateTime.Now;

                    }
                }
                else if (ShiftFrame >= ShiftPattern.Count)
                {
                    IsShifting = false;
                    TransmissionFreezeUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 200 + ShifterNewGear*50));
                }
            }
        }

        #endregion

        }
}