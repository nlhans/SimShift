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
        public int MaximumSpeed { get; private set; }

        public IDrivetrain Drivetrain { get; private set; }
        public Ets2Aero Air { get; private set; }

        // Speed / Load / [Gear]
        public Dictionary<int, Dictionary<double, int>> table;

        public ShifterTableConfiguration(ShifterTableConfigurationDefault def, IDrivetrain drivetrain, int spdPerGear)
        {
            Air = new Ets2Aero();
            Drivetrain = drivetrain;
            MaximumSpeed = 600;

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
                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;
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
                    var latestGearThatWasNotStalling = 1;

                    var shiftRpm = Drivetrain.StallRpm + (Drivetrain.MaximumRpm - Drivetrain.StallRpm) * load;
                    shiftRpm = 3000 + (Drivetrain.MaximumRpm - 3000-1000) * load;
                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < Drivetrain.StallRpm)
                        {
                            continue;
                        }
                        if (calculatedRpm < 2900) continue;
                        latestGearThatWasNotStalling = gear;
                        if (calculatedRpm > shiftRpm) continue;

                        gearSet = true;
                        table[speed].Add(load, gear + 1);
                        break;
                    }
                    if (!gearSet)
                        table[speed].Add(load, latestGearThatWasNotStalling == 1 ? 1 : latestGearThatWasNotStalling + 1);
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

                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < Drivetrain.StallRpm)
                        {
                            calculatedRpm = Drivetrain.StallRpm;
                        }
                        if (calculatedRpm < 1500) continue;
                        var pwr = Drivetrain.CalculatePower(calculatedRpm+200, load <0.2?0.2:load);

                        latestGearThatWasNotStalling = gear;
                        if (calculatedRpm > Drivetrain.MaximumRpm) continue;
                        if (gear == 0 && calculatedRpm > Drivetrain.MaximumRpm - 200) continue;
                        if (pwr  >bestPower)
                        {
                            bestPower = pwr;
                            bestPowerGear = gear;
                            gearSet = true;
                        }
                    }
                    
                    //if (speed < 30 )
                    //    table[speed].Add(load, latestGearThatWasNotStalling);
                    //else 
                        if (!gearSet)
                            table[speed].Add(load, (latestGearThatWasNotStalling == 1?1: latestGearThatWasNotStalling+1));
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

                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;

                        if (calculatedRpm < Drivetrain.StallRpm) continue;
                        if (calculatedRpm > Drivetrain.MaximumRpm) continue;

                        var thr = (load < 0.10)
                                      ? 0.10
                                      : load;

                        var pwr = Drivetrain.CalculatePower(calculatedRpm, thr);
                        var fuel = Drivetrain.CalculateFuelConsumption(calculatedRpm, thr);
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

                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;

                        if (calculatedRpm < Drivetrain.StallRpm) continue;
                        if (calculatedRpm > Drivetrain.MaximumRpm) continue;

                        var thr = Drivetrain.CalculateThrottleByPower(calculatedRpm, req);

                        if (thr > 1) continue;
                        if (thr < 0) continue;

                        if (double.IsNaN(thr) || double.IsInfinity(thr)) continue;

                        var fuel = Drivetrain.CalculateFuelConsumption(calculatedRpm, thr);

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
                    } while (endI - startI < minimum && g < Drivetrain.Gears);

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
            if (gear > Drivetrain.GearRatios.Length)
                return Drivetrain.StallRpm;
            if (gear <= 0) 
                return Drivetrain.StallRpm + 50;
            return Drivetrain.GearRatios[gear - 1] * speed * 3.6;
        }
    }
}