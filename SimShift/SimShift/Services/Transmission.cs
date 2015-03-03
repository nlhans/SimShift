using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Dialogs;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    /// Automatic shifter for vehicles. Support 1 reverse and up to 24 reverse gears using 6-speed 4-range gearbox.
    /// Uses pre-calculated 2-dimensional shifter table that maps speed and throttle position to gear.
    /// </summary>
    public class Transmission : IControlChainObj, IConfigurable
    {
        // TODO: Move to car object
        public int RangeSize = 6;
        public int Gears = 6;

        public bool Enabled { get; set; }
        public bool Active { get { return IsShifting; } }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        //
        public static bool InReverse { get; set; }

        public bool GetHomeMode { get; set; }
        public bool OverruleShifts { get; set; }

        #region Kickdown

        public bool KickdownEnable { get; private set; }
        public double KickdownTimeout { get; private set; }
        public double KickdownSpeedReset { get; private set; }
        public double KickdownPowerReset { get; private set; }
        public double KickdownRpmReset { get; private set; }

        public double KickdownLockedSpeed { get; private set; }

        public DateTime KickdownTime { get; private set; }
        public bool KickdownCooldown
        {
            get { return KickdownTime > DateTime.Now; }
            set { KickdownTime = DateTime.MinValue; }
        }

        #endregion
        #region Active Transmission Variables

        public ShiftPattern ActiveShiftPattern { get { return ShiftPatterns[ActiveShiftPatternStr]; } }

        public string ActiveShiftPatternStr;
        public Dictionary<string, ShiftPattern> ShiftPatterns = new Dictionary<string, ShiftPattern>();

        public int ShiftFrame = 0;
        public ShifterTableConfiguration configuration;

        public int GameGear { get; private set; }

        public int ShifterGear
        {
            get { return ShifterNewGear; }
        }

        public float StaticMass = 0;

        public bool IsShifting { get; private set; }

        public int ShiftCtrlOldGear { get; private set; }
        public int ShiftCtrlNewGear { get; private set; }
        public int ShiftCtrlOldRange { get; private set; }
        public int ShiftCtrlNewRange { get; private set; }

        public int ShifterOldGear
        {
            get { return ShiftCtrlOldGear + ShiftCtrlOldRange*RangeSize; }
        }

        public int ShifterNewGear
        {
            get { return ShiftCtrlNewGear + ShiftCtrlNewRange*RangeSize; }
        }

        public DateTime TransmissionFreezeUntill { get; private set; }

        public bool TransmissionFrozen
        {
            get { return TransmissionFreezeUntill > DateTime.Now; }
        }

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

        private double transmissionThrottle;
        #endregion

        public Transmission()
        {
            configuration = new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, Main.Drivetrain, 20, 0);

            LoadShiftPattern("up_1thr", "normal");
            LoadShiftPattern("up_0thr", "normal");
            LoadShiftPattern("down_1thr", "normal");
            LoadShiftPattern("down_0thr", "normal");

            // Add power shift pattern
            var powerShiftPattern = new ShiftPattern();
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(0, 1, false, false, true));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(0, 1, false, false, false));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(0, 1, false, false, true));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(1, 0.5, true, false, true));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(1, 0.5, true, false, true));
            ShiftPatterns.Add("PowerShift", powerShiftPattern);

            // Initialize all shfiting stuff.
            Shift(0, 1, "up_1thr");
            Enabled = true;
            IsShifting = false;
        }

        public void LoadShiftPatterns(List<ConfigurableShiftPattern> patterns)
        {
            ShiftPatterns.Clear();

            foreach(var p in patterns)
            {
                LoadShiftPattern(p.Region, p.File);
            }
        }

        #region Transmission Shift logics

        public void Shift(int fromGear, int toGear, string style)
        {
            if(IsShifting) return;

            if (EnableSportShiftdown && Main.GetAxisIn(JoyControls.Throttle) < 0.2)
                KickdownTime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, (int)KickdownTimeout/10));
            else
            KickdownTime = DateTime.Now.Add(new TimeSpan(0,0,0,0,(int)KickdownTimeout));

            if (PowerShift)
                style = "PowerShift";
            
            if (ShiftPatterns.ContainsKey(style))
                ActiveShiftPatternStr = style;
            else 
                ActiveShiftPatternStr = ShiftPatterns.Keys.FirstOrDefault();

            // Copy old control to new control values
            ShiftCtrlOldGear = fromGear;
            if (ShiftCtrlOldGear == -1) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear == 0) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear >= 1 && ShiftCtrlOldGear <= RangeSize) ShiftCtrlOldRange = 0;
            else if (ShiftCtrlOldGear >= RangeSize + 1 && ShiftCtrlOldGear <= 2*RangeSize) ShiftCtrlOldRange = 1;
            else if (ShiftCtrlOldGear >= 2*RangeSize + 1 && ShiftCtrlOldGear <= 3*RangeSize) ShiftCtrlOldRange = 2;
            ShiftCtrlOldGear -= ShiftCtrlOldRange*RangeSize;

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
            else if (toGear >= RangeSize + 1 && toGear <= RangeSize*2)
            {
                ShiftCtrlNewGear = toGear - RangeSize;
                ShiftCtrlNewRange = 1;
            }
            else if (toGear >= RangeSize*2 + 1 && toGear <= RangeSize*3)
            {
                ShiftCtrlNewGear = toGear - RangeSize*2;
                ShiftCtrlNewRange = 2;
            }

            ShiftFrame = 0;
            IsShifting = true;
            powerShiftStage = 0;

        }

        private void LoadShiftPattern(string pattern, string file)
        {
            // Add pattern if not existing.
            if(!ShiftPatterns.ContainsKey(pattern))
                ShiftPatterns.Add(pattern, new ShiftPattern());

            // Load configuration file
            Main.Load(ShiftPatterns[pattern], "Settings/ShiftPattern/"+file+".ini");
            return;
            /*
            switch (pattern)
            {
                    // very slow
                case "up_0thr":
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
                    ShiftPattern.Add(new ShiftPatternFrame(0.8, 0, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.5, 0.4, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0, 0.7, false, true));
                    ShiftPattern.Add(new ShiftPatternFrame(0.0,1, false, true));
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
                        ShiftPattern.Add(new ShiftPatternFrame((25 - i)/25.0, i/25.0, false, true));
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
            }*/
        }

        #endregion

        #region Transmission telemetry logic

        public void TickTelemetry(IDataMiner data)
        {
            int idealGear = data.Telemetry.Gear;

            if (data.TransmissionSupportsRanges)
                RangeSize = 6;
            else
                RangeSize = 8;
            
            // TODO: Add generic telemetry object
            GameGear = data.Telemetry.Gear;
            if (IsShifting) return;
            if (TransmissionFrozen && !GetHomeMode) return;
            if (OverruleShifts) return;
            shiftRetry = 0;

            if (GetHomeMode)
            {
                if(idealGear < 1)
                {
                    idealGear = 1;
                }

                var lowRpm = Main.Drivetrain.StallRpm*1.5;
                var highRpm = Main.Drivetrain.StallRpm*3;

                if (data.Telemetry.EngineRpm < lowRpm && idealGear > 1)
                    idealGear--;
                if (data.Telemetry.EngineRpm > highRpm && idealGear < Main.Drivetrain.Gears)
                    idealGear++;

            }
            else
            {
                if (EnableSportShiftdown)
                    transmissionThrottle = Math.Max(Main.GetAxisIn(JoyControls.Brake)*8, transmissionThrottle);
                transmissionThrottle= Math.Min(1, Math.Max(0, transmissionThrottle));
                var lookupResult = configuration.Lookup(data.Telemetry.Speed*3.6, transmissionThrottle);
                idealGear = lookupResult.Gear;
                ThrottleScale = GetHomeMode ? 1 : lookupResult.ThrottleScale;

                if (data.Telemetry.Gear == 0 && ShiftCtrlNewGear != 0)
                {
                    Debug.WriteLine("Timeout");
                    ShiftCtrlNewGear = 0;
                    TransmissionFreezeUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 250));
                    return;
                }
            }

            if (InReverse)
            {
                if (GameGear != -1 && Math.Abs(data.Telemetry.Speed) < 1)
                {
                    Debug.WriteLine("Shift from " + data.Telemetry.Gear + " to  " + idealGear);
                    Shift(data.Telemetry.Gear, -1, "up_1thr");
                }
                return;
            }

            // Kickdown?
            // What is basically does, is making this very-anxious gearbox tone down a bit in it's aggressive shift pattern.
            // With kickdown we specify 2 rules to make the gear "stick" longer with varying throttle positions.
            if (KickdownEnable)
            {
                if (KickdownCooldown)
                {
                    // We're on cooldown. Check if power/speed/rpm is able to reset this
// The scheme is as follow:
                    // if (delta of [current speed] and [speed of last shift] ) > speedReset: then reset cooldown
                    // if ([generated power this gear] / [generated power new gear]) > 1+powerReset: then reset cooldown
                    // if (currentRpm < [rpm * stationaryValue]) : then reset cooldown

                    // This makes sure the gearbox shifts down if on very low revs and almost about to stall.

                    // Note: only for speeding up
                    if ((data.Telemetry.Speed - KickdownLockedSpeed) > KickdownSpeedReset)
                    {
                        Debug.WriteLine("[Kickdown] Reset on overspeed");
                        KickdownCooldown = false;
                    }

                    var maxPwr = Main.Drivetrain.CalculateMaxPower();

                    var engineRpmCurrentGear = Main.Drivetrain.CalculateRpmForSpeed(ShifterOldGear - 1, data.Telemetry.Speed);
                    var pwrCurrentGear = Main.Drivetrain.CalculatePower(engineRpmCurrentGear, data.Telemetry.Throttle);

                    var engineRpmNewGear = Main.Drivetrain.CalculateRpmForSpeed(idealGear - 1, data.Telemetry.Speed);
                    var pwrNewGear = Main.Drivetrain.CalculatePower(engineRpmNewGear, data.Telemetry.Throttle);
                    //Debug.WriteLine("N"+pwrCurrentGear.ToString("000") + " I:" + pwrNewGear.ToString("000"));
                    // This makes sure the gearbox shifts down if on low revs and the user is requesting power from the engine
                    if (pwrNewGear / pwrCurrentGear > 1 + KickdownPowerReset &&
                        pwrNewGear / maxPwr > KickdownPowerReset)
                    {
                        Debug.WriteLine("[Kickdown] Reset on power / " + pwrCurrentGear + " / " + pwrNewGear);
                        KickdownCooldown = false;
                    }

                    // This makes sure the gearbox shifts up in decent time when reaching end of gears
                    if (Main.Drivetrain.StallRpm*KickdownRpmReset > data.Telemetry.EngineRpm)
                    {
                        Debug.WriteLine("[Kickdown] Reset on stalling RPM");
                        KickdownCooldown = true;
                    }
                }
                else
                {
                }
            }

            if (idealGear != data.Telemetry.Gear)
            {
                if (KickdownEnable && KickdownCooldown && !GetHomeMode) return;
                KickdownLockedSpeed = Main.Data.Telemetry.Speed;
                var upShift = idealGear > data.Telemetry.Gear;
                var fullThrottle = data.Telemetry.Throttle > 0.6;

                var shiftStyle = (upShift ? "up" : "down") + "_" + (fullThrottle ? "1" : "0") + "thr";

                Debug.WriteLine("Shift from " + data.Telemetry.Gear + " to  " + idealGear);
                Shift(data.Telemetry.Gear, idealGear, shiftStyle);
            }
        }

        protected double ThrottleScale { get; set; }
        protected bool EnableSportShiftdown { get; set; }
        protected bool PowerShift { get; set; }

        #endregion

        #region Control Chain Methods

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                    // Only required when changing
                case JoyControls.Throttle:
                    return Enabled|| true;

                case JoyControls.Clutch:
                    return Enabled && IsShifting;

                    // All gears.
                case JoyControls.GearR:
                case JoyControls.Gear1:
                case JoyControls.Gear2:
                case JoyControls.Gear3:
                case JoyControls.Gear4:
                case JoyControls.Gear5:
                case JoyControls.Gear6:
                    return Enabled;

                case JoyControls.GearRange2:
                case JoyControls.GearRange1:
                    return Enabled && Main.Data.Active.TransmissionSupportsRanges;

                case JoyControls.Gear7:
                case JoyControls.Gear8:
                    return Enabled && !Main.Data.Active.TransmissionSupportsRanges;

                case JoyControls.GearUp:
                case JoyControls.GearDown:
                    return Enabled ;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
 
                    transmissionThrottle = val;
                    if (transmissionThrottle > 1) transmissionThrottle = 1;
                    if (transmissionThrottle < 0) transmissionThrottle = 0;
                    lock (ActiveShiftPattern)
                    {
                        if (!Enabled)
                            return val * ThrottleScale;
                        if (shiftRetry > 0)
                            return 0;
                        if (ShiftFrame >= ActiveShiftPattern.Count)
                            return val * ThrottleScale;
                        var candidateValue= IsShifting ?ActiveShiftPattern.Frames[ShiftFrame].Throttle*val : val;
                        candidateValue *= ThrottleScale;

                        return candidateValue;
                    }
                case JoyControls.Clutch:
                    lock (ActiveShiftPattern)
                    {
                        if (ShiftFrame >= ActiveShiftPattern.Count)
                            return val;
                        return ActiveShiftPattern.Frames[ShiftFrame].Clutch;
                    }

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

                    // TODO: Move gear up/down to main object
                    /*
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
                */
                default:
                    return val;
            }
        }

        private bool GetRangeButton(int r)
        {
            if (Main.Data.Active == null || !Main.Data.Active.TransmissionSupportsRanges) return false;
            if (IsShifting && ShiftCtrlNewRange != ShiftCtrlOldRange)
            {
                // More debug values
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
                {
                    if (ShiftCtrlNewRange == 1)
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
                        if (r == 2) return engageR2Status;

                        return false;

                        // Off
                    case 2:
                        return false;

                        // Evaluate and set phase 1(on) / phase 2 (off) timings
                    default:

                        Debug.WriteLine("Shift " + ShifterOldGear + "(" + ShiftCtrlOldRange + ") to " + ShifterNewGear +
                                        "(" + ShiftCtrlNewRange + ")");
                        Debug.WriteLine("R1: " + engageR1Status + " / R2: " + engageR2Status);
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
                lock (ActiveShiftPattern)
                {
                    if (ShiftFrame >= ActiveShiftPattern.Count) return false;
                    if (ActiveShiftPattern.Frames[ShiftFrame].UseOldGear) return (b == ShiftCtrlOldGear);
                    if (ActiveShiftPattern.Frames[ShiftFrame].UseNewGear) return (b == ShiftCtrlNewGear);
                    return false;
                }
            }

            return (b == ShiftCtrlNewGear);
        }

        private int shiftRetry = 0;
        private int powerShiftStage = 0;
        private int powerShiftTimer = 0;

        public void TickControls()
        {
            if (IsShifting)
            {
                lock (ActiveShiftPattern)
                {
                    if (PowerShift)
                    {
                        var stage = powerShiftStage;
                        if (powerShiftStage == ActiveShiftPattern.Count - 2)
                        {
                            if (Main.Data.Telemetry.Gear == ShifterNewGear) powerShiftStage++;
                        }
                        else if (powerShiftStage == ActiveShiftPattern.Count - 1)
                        {
                            IsShifting = false;
                            shiftRetry = 0;
                            TransmissionFreezeUntill =
                                DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, ShiftDeadTime + 50 * ShiftCtrlNewGear));

                        }
                        else
                        {
                            powerShiftStage++;}
                        
                        if (powerShiftStage != stage)
                        {
                            powerShiftTimer = 0;
                        }
                        else
                        {
                            powerShiftTimer++;
                            if(powerShiftTimer >= 20)
                            {
                                powerShiftTimer = 0;
                                // So we are shifting, check lagging by 1, and new gear doesn't work
                                // We re-shfit
                                var tmp = ShiftFrame;
                                IsShifting = false;
                                if (shiftRetry >= 7)
                                {
                                    GameGear = shiftRetry;
                                }
                                GameGear = GameGear%18;
                                if (shiftRetry > 18+8)
                                {
                                    IsShifting = false;
                                    return;
                                }
                                Shift(GameGear, ShifterNewGear, "PowerShift");
                                ShiftFrame = 0;
                                shiftRetry++;

                                if (ShiftCtrlNewRange != ShiftCtrlOldRange)
                                {

                                    ShiftFrame = 0;
                                    RangeButtonFreeze1Untill = DateTime.Now;
                                    RangeButtonFreeze2Untill = DateTime.Now;

                                }
                            }
                        }
                        ShiftFrame = powerShiftStage;
                    }
                    else
                    {
                        ShiftFrame++;
                        if (ShiftFrame > ActiveShiftPattern.Count)
                            ShiftFrame = 0;
                        if (ShiftFrame >= ActiveShiftPattern.Count)
                        {
                            if (shiftRetry < 20 && ShiftFrame > 4 &&
                                GameGear != ShifterNewGear)
                            {
                                // So we are shifting, check lagging by 1, and new gear doesn't work
                                // We re-shfit
                                var tmp = ShiftFrame;
                                IsShifting = false;
                                if (shiftRetry >= 7)
                                    GameGear = shiftRetry;
                                Shift(GameGear, ShifterNewGear, "up_1thr");
                                ShiftFrame = 0;
                                shiftRetry++;

                                if (ShiftCtrlNewRange != ShiftCtrlOldRange)
                                {

                                    ShiftFrame = 0;
                                    RangeButtonFreeze1Untill = DateTime.Now;
                                    RangeButtonFreeze2Untill = DateTime.Now;

                                }
                            }
                            else
                            {
                                IsShifting = false;
                                shiftRetry = 0;
                                TransmissionFreezeUntill =
                                    DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, ShiftDeadTime +20*ShiftCtrlNewGear));
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs { get { return new [] { "ShiftCurve" }; } }
        public void ResetParameters()
        {
            configuration = new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, Main.Drivetrain, 10, 0);

            if (Main.Data.Active.Application == "TestDrive2")
                LoadShiftPattern("up_1thr", "fast");
            else
                LoadShiftPattern("up_1thr", "normal");

            EnableSportShiftdown = false;
            PowerShift = false;
        }

        public int speedHoldoff { get; private set; }
        public int ShiftDeadSpeed { get; private set; }
        public int ShiftDeadTime { get; private set; }
        public string GeneratedShiftTable { get; private set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "ShiftDeadSpeed":
                    ShiftDeadSpeed = obj.ReadAsInteger();
                    break;
                case "ShiftDeadTime":
                    ShiftDeadTime = obj.ReadAsInteger();
                    break;

                case "GenerateSpeedHoldoff":
                    speedHoldoff = obj.ReadAsInteger();
                    break;

                case "EnableSportShiftdown":
                    EnableSportShiftdown = obj.ReadAsInteger() == 1;
                    break;

                case "KickdownEnable":
                    KickdownEnable = obj.ReadAsString() == "1";
                    break;
                case "KickdownTimeout":
                    KickdownTimeout = obj.ReadAsDouble();
                    break;
                case "KickdownSpeedReset":
                    KickdownSpeedReset = obj.ReadAsDouble();
                    break;
                case "KickdownPowerReset":
                    KickdownPowerReset = obj.ReadAsDouble();
                    break;
                case "KickdownRpmReset":
                    KickdownRpmReset = obj.ReadAsDouble();
                    break;

                case "PowerShift":
                    PowerShift = obj.ReadAsInteger() == 1;
                    break;

                case "Generate":
                    var def = ShifterTableConfigurationDefault.PeakRpm;
                    GeneratedShiftTable = obj.ReadAsString();
                    switch (GeneratedShiftTable)
                    {
                        case "Economy":
                            def = ShifterTableConfigurationDefault.Economy;
                            break;
                        case "Efficiency":
                            def = ShifterTableConfigurationDefault.Efficiency;
                            break;
                        case "Efficiency2":
                            def = ShifterTableConfigurationDefault.PowerEfficiency;
                            break;
                        case "Opa":
                            def = ShifterTableConfigurationDefault.AlsEenOpa;
                            break;
                        case "PeakRpm":
                            def = ShifterTableConfigurationDefault.PeakRpm;
                            break;
                        case "Performance":
                            def = ShifterTableConfigurationDefault.Performance;
                            break;
                        case "Henk":
                            def = ShifterTableConfigurationDefault.Henk;
                            break;
                    }

                    configuration = new ShifterTableConfiguration(def, Main.Drivetrain, speedHoldoff, StaticMass);
                    break;
            }
        }


        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();
            obj.Add(new IniValueObject(AcceptsConfigs, "ShiftDeadSpeed", ShiftDeadSpeed.ToString()));
            obj.Add(new IniValueObject(AcceptsConfigs, "ShiftDeadTime", ShiftDeadTime.ToString()));
            obj.Add(new IniValueObject(AcceptsConfigs, "GenerateSpeedHoldoff", speedHoldoff.ToString()));
            obj.Add(new IniValueObject(AcceptsConfigs, "Generate", GeneratedShiftTable));
            //TODO: Tables not supported yet.
            return obj;
        }

        #endregion

        public void RecalcTable()
        {
            configuration = new ShifterTableConfiguration(configuration.Mode, Main.Drivetrain, configuration.SpdPerGear, configuration.Mass);
        }
    }
}