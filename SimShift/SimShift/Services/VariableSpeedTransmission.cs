using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    public class VariableSpeedTransmission : IControlChainObj
    {
        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }
        public bool Enabled { get; set; }
        public bool Active { get; private set; }
        public double SetSpeed { get; set; }

        private double variableThrottle = 0.0;
        private double variableBrake = 0.0;

        private double UserThrottle = 0;

        public VariableSpeedTransmission()
        {
            Enabled = true;
        }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Clutch:
                    case JoyControls.Brake:
                    case JoyControls.Throttle:
                    return Enabled;
                    
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
                    return Enabled;
            }
                return false;
        }

        public double GetAxis(JoyControls c, double val)
        {
            if (c == JoyControls.Brake)
                return val + variableBrake;
            if (c == JoyControls.Throttle)
            {
                UserThrottle = val*0.25f + UserThrottle*0.75f;
                switch (ShiftingPhase)
                {
                        case ShiftPhase.OffThrottle:
                        case ShiftPhase.EngageGear:
                        case ShiftPhase.EngageRange:
                        case ShiftPhase.Evaluate:
                        case ShiftPhase.OnClutch:
                        case ShiftPhase.OffClutch:
                        return 0;

                    default:
                        return variableThrottle;
                }
            }
            if (c == JoyControls.Clutch)
            {
                switch (ShiftingPhase)
                {
                    case ShiftPhase.EngageGear:
                    case ShiftPhase.EngageRange:
                    case ShiftPhase.Evaluate:
                    case ShiftPhase.OnClutch:
                        return 1;

                    default:
                        return val;
                }
            }
            return val;
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

                default:
                    return val;
            }
        }

        private int DeductRangeFromGear(int gr)
        {
            if (gr >= 1 && gr <= 6) return 1;
            else if (gr >= 7 && gr <= 12) return 2;
            else if (gr >= 13 && gr <= 18) return 3;
            else if (gr >= 19 && gr <= 24) return 4;
            else return -1;
        }

        private bool RangeSwitchTruthTable(int button, int old, int nw)
        {
            switch(old)
            {
                case 1:
                    if (nw == 1) return false;
                    if (nw == 2) return button == 1;
                    if (nw == 3) return button == 2;
                    if (nw == 4) return button == 1 || button == 2;
                    break;
                case 2:
                    if (nw == 1) return button == 1;
                    if (nw == 2) return false;
                    if (nw == 3) return button == 1 || button == 2;
                    if (nw == 4) return button == 2;
                    break;
                case 3:
                    if (nw == 1) return button == 2;
                    if (nw == 2) return button == 2 || button == 1;
                    if (nw == 3) return false;
                    if (nw == 4) return button == 1;
                    break;
                case 4:
                    if (nw == 1) return button == 1 || button == 2;
                    if (nw == 2) return button == 2;
                    if (nw == 3) return button == 1;
                    if (nw == 4) return false;
                    break;

            }
            return false;
        }

        private bool GetRangeButton(int bt)
        {
            var currentRange = LastSeenRange;
            var newRange = DeductRangeFromGear(Gear);

            if (currentRange != newRange)
            {
                if (ShiftingPhase == ShiftPhase.EngageRange)
                    return RangeSwitchTruthTable(bt, currentRange, newRange);
                else
                    return false;
            }
            return false;
        }

        private bool GetShiftButton(int bt)
        {
            if (bt == -1)
                return Gear == -1 && ShiftingPhase != ShiftPhase.EngageRange;
            else
            {
                var range = DeductRangeFromGear(Gear) - 1;
                var activeGearInRange = Gear - range*6;
                return activeGearInRange == bt && ShiftingPhase != ShiftPhase.EngageRange;
            }
        }

        public int LastSeenRange { get; set; }
        public int Gear { get; set; }

        private ShiftPhase ShiftingPhase { get; set; }
        public bool Reverse { get; set; }
        public bool Efficiency { get; set; }

        public void TickControls()
        {
        }

        private double staticThrError = 0;
        private DateTime lastShifterTick = DateTime.Now;
        private int shiftingRetry=0;
        public static double reqpower = 0;
        public bool fast = false;
        private bool wasEfficiency = false;
        private double lastSpeedError = 0;

        public void TickTelemetry(IDataMiner data)
        {
            bool copyTargetThr = false;

            /** VARIABLE SPEED CONTROL **/
            var actualSpeed = data.Telemetry.Speed * 3.6;

            double thrP, thrI;
            if (data.Telemetry.Gear == -1)
            {
                SetSpeed = Main.GetAxisIn(JoyControls.VstLever) * 50;
                actualSpeed *= -1;

                thrP = 0.1;
                thrI = 0;

            }
            else
            {
                SetSpeed = Main.GetAxisIn(JoyControls.VstLever)*((fast) ? 200 : 100);

                thrP = 0.015 + 0.15 * actualSpeed / 120;
                thrI = 0.02 - 0.015 * actualSpeed / 120;
                if (Efficiency)
                {
                    thrP *= 0.5;
                    thrI = 0.00025;
                }
                if (Efficiency != wasEfficiency)
                {
                    copyTargetThr = true;
                    staticThrError = variableThrottle-2*thrP*lastSpeedError;
                }
                wasEfficiency = Efficiency;
            }
            var speedErrorThr = SetSpeed - actualSpeed;
            staticThrError += speedErrorThr*thrI;
            if (staticThrError > 0.8) staticThrError = 0.8;
            if (staticThrError < -0.8) staticThrError = -0.8;
            lastSpeedError = speedErrorThr;
            var oldThr = variableThrottle;
            if (copyTargetThr)
            {
                variableThrottle = thrP * speedErrorThr;
                variableThrottle *= UserThrottle;
                
                // Theoratical value required to copy last throttle
                staticThrError = (1 - variableThrottle)/UserThrottle;
               
                //Deduce at low speeds
                var deductor = actualSpeed/50;
                if (deductor > 1) deductor = 1;
                if (deductor < 0.01) deductor = 0.01;
                staticThrError *= deductor;

                // Add it.
                variableThrottle += staticThrError;
            }
            else
            {
                variableThrottle = thrP * speedErrorThr + staticThrError;
                variableThrottle *= UserThrottle;
            }
            if (variableThrottle > 1) variableThrottle = 1;
            if (variableThrottle < 0) variableThrottle = 0;

            var speedErrorBr = (actualSpeed - 3) - SetSpeed;
            if (speedErrorBr < 0) speedErrorBr = 0;
            if (actualSpeed < 50)
                speedErrorBr *= (50 - actualSpeed) / 15 + 1;
            variableBrake = 0.01 * speedErrorBr * speedErrorBr;
            if (variableBrake > 0.2) variableBrake = 0.2;
            if (variableBrake < 0) variableBrake = 0;

            if (variableBrake > 0.01)
                variableThrottle = 0;
            /** TRANSMISSION **/
            if (Main.Data.Active.Application == "eurotrucks2")
            {
                var ets2 = (Ets2DataMiner) Main.Data.Active;
                if (ets2.MyTelemetry.flags[1] == 0 && !fast)
                    Efficiency = true;
                else
                {
                    if (Efficiency)
                    {
                        if ((SetSpeed - actualSpeed) > 5)
                        {
                            Efficiency = false;
                        }
                    }
                    else
                    {
                        if ((SetSpeed - actualSpeed) < 2)
                        {
                            Efficiency = true;
                        }
                    }
                }
            }
            //Efficiency = (SetSpeed - actualSpeed) < 5;
            if (DateTime.Now.Subtract(lastShifterTick).TotalMilliseconds > 40)
            {
                Active = ShiftingPhase == ShiftPhase.None ? false : true;
                lastShifterTick = DateTime.Now;
                switch (ShiftingPhase)
                {
                    case ShiftPhase.WaitButton:
                        if (Main.GetButtonIn(JoyControls.GearUp) == false &&
                            Main.GetButtonIn(JoyControls.GearDown) == false)
                        {
                            ShiftingPhase = ShiftPhase.None;
                        }
                        break;

                    case ShiftPhase.None:

                        // Reverse pressed?
                        if (Main.GetButtonIn(JoyControls.VstChange))
                        {
                            fast = !fast;
                            ShiftingPhase = ShiftPhase.WaitButton;
                        }
                        if (Main.GetButtonIn(JoyControls.GearDown))
                        {
                            Reverse = !Reverse;
                            ShiftingPhase = ShiftPhase.WaitButton;
                        }

                        if (Reverse)
                        {
                            if (data.Telemetry.Gear != -1)
                            {
                                Gear = -1;
                                ShiftingPhase = ShiftPhase.OffThrottle;
                            }
                        }
                        else
                        {
                            // Do nothing
                            if (data.Telemetry.Gear != Gear || Gear == 0)
                            {
                                if (Gear == 0)
                                    Gear++;
                                if (Gear == -1)
                                    Gear = 1;
                                ShiftingPhase = ShiftPhase.OffThrottle;
                            }
                            else
                            {
                                var curPower = Main.Drivetrain.CalculatePower(data.Telemetry.EngineRpm,
                                    data.Telemetry.Throttle);
                                if (curPower < 1) curPower = 1;
                                var curEfficiency = Main.Drivetrain.CalculateFuelConsumption(data.Telemetry.EngineRpm,
                                    data.Telemetry.Throttle)/curPower;
                                var reqPower = curPower*(variableThrottle - 0.5)*2;
                                if (reqPower < 25) reqPower = 25;
                                if (reqPower > 0.5*Main.Drivetrain.CalculateMaxPower())
                                    reqPower = 0.5*Main.Drivetrain.CalculateMaxPower();
                                reqpower = reqPower;
                                int maxgears = Main.Drivetrain.Gears;
                                var calcEfficiency = Efficiency ? double.MaxValue : 0;
                                var calcEfficiencyGear = -1;
                                var calcThrottle = variableThrottle;
                                var calcPower = curPower;

                                var allStalled = true;
                                for (int k = 0; k < maxgears; k++)
                                {
                                    if (maxgears >= 12 && ( k == 5)) continue;
                                    if (maxgears >= 10 && (k == 1 || k == 3)) continue;
                                    if (!Efficiency && k < 3) continue;

                                    // Always pick best efficient gear with power requirement met
                                    var rpm = Main.Drivetrain.CalculateRpmForSpeed(k, data.Telemetry.Speed);
                                    var orpm = Main.Drivetrain.CalculateRpmForSpeed(k, data.Telemetry.Speed);
                                    var estimatedPower = Main.Drivetrain.CalculatePower(rpm, variableThrottle);
                                    
                                    // RPM increase linear to throttle:
                                    rpm += estimatedPower/1200*190*variableThrottle;

                                    if (rpm < Main.Drivetrain.StallRpm && k > 0)
                                        continue;
                                    allStalled = false;
                                    if (orpm > Main.Drivetrain.MaximumRpm)
                                        continue;

                                    var thr = Main.Drivetrain.CalculateThrottleByPower(rpm, reqPower);
                                    if (thr > 1) thr = 1;
                                    if (thr < 0) thr = 0;
                                    var eff = Main.Drivetrain.CalculateFuelConsumption(rpm, thr)/reqPower;
                                    var pwr = Main.Drivetrain.CalculatePower(rpm, variableThrottle);

                                    if (Efficiency)
                                    {
                                        if (calcEfficiency > eff && eff*1.1 < curEfficiency)
                                        {
                                            calcEfficiency = eff;
                                            calcEfficiencyGear = k;
                                            calcThrottle = thr;
                                            calcPower = pwr;
                                        }
                                    }
                                    else
                                    {
                                        if (pwr > calcEfficiency)
                                        {
                                            calcEfficiency = pwr;
                                            calcEfficiencyGear = k;
                                            calcPower = pwr;
                                        }
                                    }
                                }

                                if (allStalled)
                                    if (maxgears >= 10)
                                    Gear = 3;
                                    else
                                    {
                                        Gear = 1;
                                    }
                                else if (calcEfficiencyGear >= 0 &&
                                    calcEfficiencyGear+1 != Gear)
                                {

                                    // Hysterisis
                                    if (Math.Abs(curPower - calcPower) > 25)
                                        Gear = calcEfficiencyGear+1;
                                }
                                if (Efficiency)
                                {
                                    //variableThrottle = Main.Drivetrain.CalculateThrottleByPower(
                                    //    data.Telemetry.EngineRpm,
                                    //    reqPower);
                                }
                                else
                                {
                                    //
                                }
                                if (Gear > 0 && Gear != data.Telemetry.Gear)
                                    ShiftingPhase = ShiftPhase.OffThrottle;
                            }
                        }
                        break;

                    case ShiftPhase.OffThrottle:
                        ShiftingPhase++;
                        break;

                    case ShiftPhase.OnClutch:
                        ShiftingPhase++;
                        break;

                    case ShiftPhase.EngageRange:
                        ShiftingPhase++;
                        break;

                    case ShiftPhase.EngageGear:
                        ShiftingPhase++;
                        break;

                    case ShiftPhase.Evaluate:
                        if (Gear == data.Telemetry.Gear)
                        {
                            LastSeenRange = DeductRangeFromGear(Gear);
                            ShiftingPhase++;
                        }
                        else
                        {
                            shiftingRetry++;
                            if (shiftingRetry > 50)
                            {
                                if (Gear > 0)
                                    Gear--;
                                else
                                    Gear = -1;
                                shiftingRetry = 0;
                                ShiftingPhase = ShiftPhase.EngageGear;
                            }
                            else                            if (shiftingRetry > 2)
                            {
                                LastSeenRange++;
                                LastSeenRange = LastSeenRange % 4;
                                ShiftingPhase = ShiftPhase.OnClutch;
                            }
                            else
                            {
                                ShiftingPhase = ShiftPhase.EngageGear;
                            }
                        }
                        break;

                    case ShiftPhase.OffClutch:
                        ShiftingPhase++;
                        break;

                    case ShiftPhase.OnThrottle:
                        shiftingRetry = 0;
                        ShiftingPhase = ShiftPhase.Cooldown;
                        break;

                        case ShiftPhase.Cooldown:
                        shiftingRetry++;
                        if (shiftingRetry > 4)
                        {
                            shiftingRetry = 0;
                            ShiftingPhase = ShiftPhase.None;
                        }
                        break;
                }
            }


        }
    }

    public enum ShiftPhase
    {
        WaitButton,
        None,
        OffThrottle,
        OnClutch,
        EngageRange,
        EngageGear,
        Evaluate,
        OffClutch,
        OnThrottle,
        Cooldown
    }
}
