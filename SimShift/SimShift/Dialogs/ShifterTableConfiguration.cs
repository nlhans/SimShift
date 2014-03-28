using System;
using System.Collections.Generic;
using System.Linq;
using SimShift.Models;
using SimShift.Services;
using SimShift.Utils;

namespace SimShift.Dialogs
{
    public class ShifterTableConfiguration
    {
        public int Gears { get; private set; }
        public double[] GearRatios { get; private set; }

        public int MaximumSpeed { get; private set; }

        public double IdleRpm { get; private set; }
        public double PeakRpm { get; private set; }
        public double MaximumRpm { get; private set; }

        public Ets2Engine Engine { get; private set; }
        public Ets2Aero Air { get; private set; }

        // Speed / Load / [Gear]
        public Dictionary<int, Dictionary<double, int>> table;

        public ShifterTableConfiguration(ShifterTableConfigurationDefault def, int spdPerGear)
        {
            Air = new Ets2Aero();
            IdleRpm = 900;
            PeakRpm = 1750;
            MaximumRpm = 2100;
            bool volvo = false;
            bool kenworth = false;
            MaximumSpeed = 300;
            if (true)
            {
                Engine = new Ets2Engine(500);
                Gears = 7;
                IdleRpm = 900;
                PeakRpm = 6000;
                MaximumRpm = 6700;

                MaximumSpeed = 400;

                GearRatios = new double[7]
                                 {
                                     332, 226.27, 149.64, 106.1, 77.55, 63.63, 56.65
                                 };

                for (int i = 0; i < Gears; i++)
                    GearRatios[i] /= 3.6;

            }
            else if (kenworth)
            {
                Engine = new Ets2Engine(5000);
                Gears = 18;
                GearRatios = new double[18]
                                 {
                                     14.89, 12.41, 10.4, 8.66, 7.32, 6.09, 5.05, 4.21, 3.54, 2.95, 2.47, 2.06, 1.74,
                                     1.45, 1.2, 1.00, 0.84, 0.70
                                 };

                for (int i = 0; i < Gears; i++)
                    GearRatios[i] *= 3.36*18.3/3.6; // for every m/s , this much RPM's
            }
            else if (volvo)
            {
                Engine = new Ets2Engine(3550);
                Gears = 12;
                GearRatios = new double[12]
                                 {
                                     11.73, 9.21, 7.09, 5.57, 4.35, 3.41, 2.7, 2.12, 1.63, 1.28, 1.0, 0.78
                                 };
                for (int i = 0; i < Gears; i++)
                    GearRatios[i] *= 3.4*18.3/3.6; // for every m/s , this much RPM's
            }
            else
            {
                Engine = new Ets2Engine(9500);
                Gears = 15;
                GearRatios = new double[15]
                                 {
                                     9.16, 7.33, 5.82, 4.66, 3.72, 3, 2.44, 1.96, 1.55, 1.24, 1, 0.8, 0.71, 0.65, 0.6
                                 };
                for (int i = 0; i < Gears; i++)
                    GearRatios[i] *= 2.8*18.3/3.6; // for every m/s , this much RPM's
            }

            switch (def)
            {
                case ShifterTableConfigurationDefault.PeakRpm:
                    DefaultByPeakRpm();
                    break;
                case ShifterTableConfigurationDefault.Performance:
                    DefaultByPowerPerformance();
                    break;
                case ShifterTableConfigurationDefault.Economy:
                    DefaultByPowerEconomy();
                    break;
                case ShifterTableConfigurationDefault.Efficiency:
                    DefaultByPowerEfficiency();
                    break;
                case ShifterTableConfigurationDefault.AlsEenOpa:
                    DefaultByOpa();
                    break;
            }

            if (spdPerGear > 0)
                MinimumSpeedPerGear(spdPerGear);


        }

        public void DefaultByOpa()
        {
            table = new Dictionary<int, Dictionary<double, int>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                table.Add(speed, new Dictionary<double, int>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    var gearSet = false;
                    var shiftRpm = 600 + 700*load;
                    var highestGearBeforeStalling = 0;
                    for (int gear = 0; gear < Gears; gear++)
                    {
                        var calculatedRpm = GearRatios[gear] * speed;
                        if (calculatedRpm < 600) continue;
                        highestGearBeforeStalling = gear;
                        if (calculatedRpm > shiftRpm) continue;

                        gearSet = true;
                        table[speed].Add(load, gear + 1);
                        break;
                    }
                    if (!gearSet)
                        table[speed].Add(load, highestGearBeforeStalling+1);
                }
            }

        }

        public void DefaultByPeakRpm()
        {
            table = new Dictionary<int, Dictionary<double, int>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                table.Add(speed, new Dictionary<double, int>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    var gearSet = false;
                    
                    var shiftRpm = IdleRpm + (MaximumRpm - IdleRpm) * load;
                    for (int gear = 0; gear < Gears; gear++)
                    {
                        var calculatedRpm = GearRatios[gear] * speed;
                        if (calculatedRpm < Engine.StallRpm)
                        {
                            continue;
                        }
                        if (calculatedRpm < 900) continue;
                        if (calculatedRpm > shiftRpm) continue;

                        gearSet = true;
                        table[speed].Add(load, gear + 1);
                        break;
                    }
                    if (!gearSet)
                        table[speed].Add(load, speed <= 12 ? 1 : Gears);
                }
            }

        }

        public void DefaultByPowerPerformance()
        {
            table = new Dictionary<int, Dictionary<double, int>>();
            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                table.Add(speed, new Dictionary<double, int>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    var gearSet = false;

                    var bestPower = double.MinValue;
                    var bestPowerGear = 0;
                    var latestGearThatWasNotStalling = 1;

                    for (int gear = 0; gear < Gears; gear++)
                    {
                        var calculatedRpm = GearRatios[gear] * speed;
                        if (calculatedRpm < Engine.StallRpm)
                        {
                            calculatedRpm = Engine.StallRpm;
                        }
                        if (calculatedRpm < 4500) continue;
                        var pwr = Engine.CalculatePower(calculatedRpm+200, load <0.2?0.2:load);

                        latestGearThatWasNotStalling = gear;
                        if (calculatedRpm > this.MaximumRpm) continue;
                        if (pwr  >bestPower)
                        {
                            bestPower = pwr;
                            bestPowerGear = gear;
                            gearSet = true;
                        }
                    }
                    if (!gearSet)
                        table[speed].Add(load, latestGearThatWasNotStalling);
                    else
                    {
                        table[speed].Add(load, bestPowerGear + 1);
                    }
                }
            }
        }

        public void DefaultByPowerEfficiency()
        {
            table = new Dictionary<int, Dictionary<double, int>>();
            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                table.Add(speed, new Dictionary<double, int>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    var gearSet = false;
                    double req = load * 500;
                    if (Math.Abs(load - 1.0) < 0.01 && speed < 6)
                    {

                    }
                    var bestFuelEfficiency = double.MinValue;
                    var bestFuelGear = 0;

                    for (int gear = 0; gear < Gears; gear++)
                    {
                        var calculatedRpm = GearRatios[gear] * speed;

                        if (calculatedRpm < Engine.StallRpm) continue;
                        if (calculatedRpm > Engine.MaximumRpm) continue;

                        var thr = (load < 0.10)
                                      ? 0.10
                                      : load;

                        var pwr = Engine.CalculatePower(calculatedRpm, thr);
                        var fuel = Engine.CalculateFuelConsumption(calculatedRpm, thr);
                        var efficiency = pwr / fuel;

                        if (efficiency > bestFuelEfficiency)
                        {
                            bestFuelEfficiency = efficiency;
                            bestFuelGear = gear;
                            gearSet = true;
                        }
                    }
                    if (!gearSet)
                        table[speed].Add(load, 1);
                    else
                    {
                        table[speed].Add(load, bestFuelGear + 1);
                    }
                }
            }

        }
        public void DefaultByPowerEconomy()
        {
            table = new Dictionary<int, Dictionary<double, int>>();
            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                table.Add(speed, new Dictionary<double, int>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    var gearSet = false;
                    double req = load*600;

                    var bestFuelEfficiency = double.MaxValue;
                    var bestFuelGear = 0;

                    for (int gear = 0; gear < Gears; gear++)
                    {
                        var calculatedRpm = GearRatios[gear] * speed;

                        if (calculatedRpm < Engine.StallRpm) continue;
                        if (calculatedRpm > Engine.MaximumRpm) continue;

                        var thr = Engine.CalculateThrottleByPower(calculatedRpm, req);

                        if (thr > 1) continue;
                        if (thr < 0) continue;

                        if (double.IsNaN(thr) || double.IsInfinity(thr)) continue;

                        var fuel = Engine.CalculateFuelConsumption(calculatedRpm, thr);

                        if(bestFuelEfficiency > fuel)
                        {
                            bestFuelEfficiency = fuel;
                            bestFuelGear = gear;
                            gearSet = true;
                        }
                    }
                    if (!gearSet)
                        table[speed].Add(load, 1);
                    else
                    {
                        table[speed].Add(load, bestFuelGear + 1);
                    }
                }
            }

        }

        public void MinimumSpeedPerGear(int minimum)
        {
            var loads = table.FirstOrDefault().Value.Keys.ToList();
            var speeds = table.Keys.ToList();
            foreach(var load in loads)
            {
                for (int i = 0; i < speeds.Count; i++)
                {
                    int startI = i;
                    int endI = i;

                    int g = table[speeds[i]][load];

                    do
                    {
                        while (endI < speeds.Count-1 && table[speeds[endI]][load] == g)
                            endI++;
                        g++;
                    } while (endI-startI < minimum && g < Gears);

                    for (int j = startI; j <= endI; j++)
                        table[speeds[j]][load] = g-1;

                    i = endI;
                }
            }
        }

        public ShifterTableLookupResult Lookup(double speed, double load)
        {
            var speedA = 0.0;
            var speedB = 0.0;
            var loadA = 0.0;
            var loadB = 0.0;

            foreach (var spd in table.Keys)
            {
                if (spd >= speed && speedA <= speed)
                {
                    speedB = spd;
                    break;
                }
                speedA = spd;
            }


            foreach (var ld in table[(int)speedA].Keys)
            {
                if (ld >= load && loadA <= load)
                {
                    loadB = ld;
                    break;
                }
                loadA = ld;
            }

            if (speedB == speedA)
            {
                speedA = table.Keys.FirstOrDefault();
                speedB = table.Keys.Skip(1).FirstOrDefault();
            }
            if (loadB == loadA)
            {
                loadA = table[(int)speedA].Keys.FirstOrDefault();
                loadB = table[(int)speedA].Keys.Skip(1).FirstOrDefault();
            }

            var gear = 1.0/(speedB - speedA)/(loadB - loadA)*(
                                                                 table[(int)speedA][loadA] * (speedB - speed) * (loadB - load) +
                                                                 table[(int)speedB][loadA] * (speed - speedA) * (loadB - load) +
                                                                 table[(int)speedA][loadB] * (speedB - speed) * (load - loadA) +
                                                                 table[(int)speedB][loadB] * (speed - speedA) * (load - loadA));
            if (double.IsNaN(gear))
                gear = 1;
            // Look up the closests RPM.
            var closestsSpeed = table.Keys.OrderBy(x => Math.Abs(speed - x)).FirstOrDefault();
            var closestsLoad = table[closestsSpeed].Keys.OrderBy(x => Math.Abs(x-load)).FirstOrDefault();
            
            //return new ShifterTableLookupResult((int)Math.Round(gear), closestsSpeed, closestsLoad);
            return new ShifterTableLookupResult(table[closestsSpeed][closestsLoad], closestsSpeed, closestsLoad);
        }

        public double RpmForSpeed(float speed, int gear)
        {
            if (gear > GearRatios.Length) 
                return IdleRpm;
            if (gear <= 0) 
                return Engine.StallRpm + 50;
            return GearRatios[gear - 1]*speed*3.6;
        }
    }
}