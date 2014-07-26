using System;
using System.Collections.Generic;
using System.IO;
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
        public Dictionary<int, Dictionary<double, int>> tableGear;
        public Dictionary<int, Dictionary<double, double>> tableThrottle;

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
                case ShifterTableConfigurationDefault.Henk:
                    DefaultByHenk();
                    break;

                case ShifterTableConfigurationDefault.PowerEfficiency:
                    DefaultByPowerEfficiency2();
                    break;
            }

            if (spdPerGear > 0)
                MinimumSpeedPerGear(spdPerGear);

            string l = "";
            for(var r = 0; r < 2500; r+=10)
            {
                var fuel=Drivetrain.CalculateFuelConsumption(r, 1);
                var ratio = drivetrain.CalculatePower(r, 1)/fuel;
                
                l +=  r + "," + Drivetrain.CalculatePower(r, 1) + "," + Drivetrain.CalculatePower(r, 0) + ","+fuel+","+ratio+"\r\n";
            }
            //File.WriteAllText("./ets2engine.csv", l);
        }

        private void DefaultByPowerEfficiency2()
        {
            tableGear = new Dictionary<int, Dictionary<double, int>>();
            tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            if (Drivetrain.Gears == 0) return;
            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                tableGear.Add(speed, new Dictionary<double, int>());
                tableThrottle.Add(speed, new Dictionary<double, double>());

                Dictionary<int, float> pwrPerGear = new Dictionary<int, float>();

                // Populate:
                for (int gear = 0; gear < Drivetrain.Gears; gear++)
                {
                    var calculatedRpm = Drivetrain.GearRatios[gear]*speed;
                    var power = (float) Drivetrain.CalculatePower(calculatedRpm, 1);
                    pwrPerGear.Add(gear, power);
                }

                var maxPwrAvailable = pwrPerGear.Values.Max()*0.85;

                for (var load = 0.0; load <= 1.0; load += 0.1)
                {

                    Dictionary<int, float> efficiencyPerGear = new Dictionary<int, float>();
                    var highestGearBeforeStalling = 0;

                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear]*speed;
                        if (calculatedRpm > Drivetrain.StallRpm) highestGearBeforeStalling = gear;
                        var power = (float) Drivetrain.CalculatePower(calculatedRpm, 1);
                        var fuel = (float) Drivetrain.CalculateFuelConsumption(calculatedRpm, Math.Max(0.05,load));
                        efficiencyPerGear.Add(gear, fuel/power);
                    }
                    var bestGear = highestGearBeforeStalling;
                    var bestGearV = 100.0f;
                    foreach (var kvp in efficiencyPerGear)
                    {
                        if (kvp.Value < bestGearV && kvp.Value>0)
                        {
                            bestGearV = kvp.Value;
                            bestGear = kvp.Key;
                        }
                    }
                    var actualRpm = Drivetrain.GearRatios[bestGear]*speed;

                    var reqThr = Drivetrain.CalculateThrottleByPower(actualRpm, load * maxPwrAvailable);
                    var thrScale = reqThr/Math.Max(load,0.1);
                    if (thrScale > 1.5) thrScale = 1.5;
                    tableGear[speed].Add(load, bestGear+1);
                    tableThrottle[speed].Add(load,thrScale);

                }
            }
        }

        public void DefaultByHenk()
        {
            var shiftRpmHigh = new float[12] { 1000, 1000, 1000, 1100, 1700, 1900,
                2000, 2000, 1900, 1800, 1500, 1300 };
            var shiftRpmLow = new float[12]
                                   {750, 750, 750, 750, 750, 750, 
                                       750, 800, 850, 800, 850, 900};

            tableGear = new Dictionary<int, Dictionary<double, int>>();
            tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                tableGear.Add(speed, new Dictionary<double, int>());
                tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    var gearSet = false;
                    var smallestDelta = double.MaxValue;
                    var smallestDeltaGear = 0;
                    var highestGearBeforeStalling = 0;
                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < shiftRpmLow[gear]) continue;
                        highestGearBeforeStalling = gear;
                        if (calculatedRpm > shiftRpmHigh[gear]) continue;

                        var driveRpm = shiftRpmLow[gear] + (shiftRpmHigh[gear] - shiftRpmLow[gear])*load;
                        var delta = Math.Abs(calculatedRpm - driveRpm);

                        if(delta < smallestDelta)
                        {
                            smallestDelta = delta;
                            smallestDeltaGear = gear;
                            gearSet = true;
                        }
                    }
                    if (gearSet)
                        tableGear[speed].Add(load, smallestDeltaGear + 1);
                    else
                        tableGear[speed].Add(load, highestGearBeforeStalling + 1);

                    tableThrottle[speed].Add(load, 1);
                }
            }
        }

        public void DefaultByOpa()
        {
            tableGear = new Dictionary<int, Dictionary<double, int>>();
            tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                tableGear.Add(speed, new Dictionary<double, int>());
                tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    var shiftRpm = 800 + 600*load;
                    var highestGearBeforeStalling = 0;
                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < 800) continue;
                        highestGearBeforeStalling = gear;
                        if (calculatedRpm > shiftRpm) continue;

                        gearSet = true;
                        tableGear[speed].Add(load, gear + 1);
                        break;
                    }
                    if (!gearSet)
                        tableGear[speed].Add(load, highestGearBeforeStalling+1);
                }
            }

        }

        public void DefaultByPeakRpm()
        {
            tableGear = new Dictionary<int, Dictionary<double, int>>();
            tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                tableGear.Add(speed, new Dictionary<double, int>());
                tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    var latestGearThatWasNotStalling = 1;

                    var shiftRpm = Drivetrain.StallRpm + (Drivetrain.MaximumRpm - 300 - Drivetrain.StallRpm) * load;
                    //shiftRpm = 3000 + (Drivetrain.MaximumRpm - 3000-1000) * load;
                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < Drivetrain.StallRpm*1.75)
                        {
                            continue;
                        }

                        latestGearThatWasNotStalling = gear;
                        if (calculatedRpm > shiftRpm) continue;

                        gearSet = true;
                        tableGear[speed].Add(load, gear + 1);
                        break;
                    }
                    if (!gearSet)
                        tableGear[speed].Add(load, latestGearThatWasNotStalling == 1 ? 1 : latestGearThatWasNotStalling + 1);
                }
            }

        }

        public void DefaultByPowerPerformance()
        {
            tableGear = new Dictionary<int, Dictionary<double, int>>();
            tableThrottle = new Dictionary<int, Dictionary<double, double>>();
            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                tableGear.Add(speed, new Dictionary<double, int>());
                tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    tableThrottle[speed].Add(load, 1);
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
                        if (calculatedRpm < 1200) continue;
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
                    //    tableGear[speed].Add(load, latestGearThatWasNotStalling);
                    //else 
                        if (!gearSet)
                            tableGear[speed].Add(load, (latestGearThatWasNotStalling == 1?1: latestGearThatWasNotStalling+1));
                    else
                    {
                        tableGear[speed].Add(load, bestPowerGear + 1);
                    }
                }
            }
        }

        public void DefaultByPowerEfficiency()
        {
            tableGear = new Dictionary<int, Dictionary<double, int>>();
            tableThrottle = new Dictionary<int, Dictionary<double, double>>();
            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                tableGear.Add(speed, new Dictionary<double, int>());
                tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    var bestFuelEfficiency = double.MinValue;
                    var bestFuelGear = 0;

                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;

                        if (calculatedRpm < Drivetrain.StallRpm * 1.25) continue;
                        if (calculatedRpm > Drivetrain.MaximumRpm) continue;

                        var thr = (load < 0.05)
                                      ? 0.05
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
                    {
                        if (Drivetrain is Ets2Drivetrain)
                            tableGear[speed].Add(load, 3);
                        else
                            tableGear[speed].Add(load, 1);
                    }
                    else
                    {
                        if (Drivetrain is Ets2Drivetrain)
                            bestFuelGear = Math.Max(2, bestFuelGear);
                        tableGear[speed].Add(load, bestFuelGear + 1);
                    }
                }
            }

        }
        public void DefaultByPowerEconomy()
        {
            var maxPwr =  Drivetrain.CalculateMaxPower() * 0.75;
            maxPwr = 500;
            tableGear = new Dictionary<int, Dictionary<double, int>>();
            tableThrottle = new Dictionary<int, Dictionary<double, double>>();
            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= MaximumSpeed; speed += 1)
            {
                tableGear.Add(speed, new Dictionary<double, int>());
                tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    double req = Math.Max(25,load*maxPwr);

                    var bestFuelEfficiency = double.MaxValue;
                    var bestFuelGear = 0;
                    var highestValidGear =11;

                    for (int gear = 0; gear < Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = Drivetrain.GearRatios[gear] * speed;

                        if (calculatedRpm <= Drivetrain.StallRpm*1.033333)
                        {
                            highestValidGear = 0;
                            continue;
                        }
                        if (calculatedRpm >= Drivetrain.MaximumRpm) continue;

                        var thr = Drivetrain.CalculateThrottleByPower(calculatedRpm, req);

                        if (thr > 1) continue;
                        if (thr < 0) continue;

                        if (double.IsNaN(thr) || double.IsInfinity(thr)) continue;

                        var fuel = Drivetrain.CalculateFuelConsumption(calculatedRpm, thr);
                        
                        if(bestFuelEfficiency >= fuel)
                        {
                            bestFuelEfficiency = fuel;
                            bestFuelGear = gear;
                            gearSet = true;
                        }
                    }
                    if (!gearSet)
                    {
                        if (Drivetrain is Ets2Drivetrain)
                            highestValidGear = Math.Max(2, highestValidGear);
                        tableGear[speed].Add(load, 1+highestValidGear);
                    }
                    else
                    {
                        bestFuelGear = Math.Max(2, bestFuelGear);
                        if (Drivetrain is Ets2Drivetrain)
                            highestValidGear = Math.Max(2, bestFuelGear);
                        tableGear[speed].Add(load, bestFuelGear + 1);
                    }
                }
            }

        }

        public void MinimumSpeedPerGear(int minimum)
        {
            if (Drivetrain.Gears == 0) return;
            var loads = tableGear.FirstOrDefault().Value.Keys.ToList();
            var speeds = tableGear.Keys.ToList();
            // Clean up first gear.
            var lowestFirstGear = tableGear[minimum][loads.LastOrDefault()];
            // Set up for all gears
            for(int k = 0; k < minimum+2;k++)
            {
                foreach(var load in loads)
                {
                    tableGear[k][load] = lowestFirstGear;
                }
            }

            foreach(var load in loads)
            {
                for (int i = 0; i < speeds.Count; i++)
                {
                    int startI = i;
                    int endI = i;

                    int g = tableGear[speeds[i]][load];

                    do
                    {
                        while (endI < speeds.Count-1 && tableGear[speeds[endI]][load] == g)
                            endI++;
                        g++;
                    } while (endI - startI < minimum && g < Drivetrain.Gears);

                    for (int j = startI; j <= endI; j++)
                        tableGear[speeds[j]][load] = g-1;

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

            foreach (var spd in tableGear.Keys)
            {
                if (spd >= speed && speedA <= speed)
                {
                    speedB = spd;
                    break;
                }
                speedA = spd;
            }


            foreach (var ld in tableGear[(int)speedA].Keys)
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
                speedA = tableGear.Keys.FirstOrDefault();
                speedB = tableGear.Keys.Skip(1).FirstOrDefault();
            }
            if (loadB == loadA)
            {
                loadA = tableGear[(int)speedA].Keys.FirstOrDefault();
                loadB = tableGear[(int)speedA].Keys.Skip(1).FirstOrDefault();
            }

            var gear = 1.0/(speedB - speedA)/(loadB - loadA)*(
                                                                 tableGear[(int)speedA][loadA] * (speedB - speed) * (loadB - load) +
                                                                 tableGear[(int)speedB][loadA] * (speed - speedA) * (loadB - load) +
                                                                 tableGear[(int)speedA][loadB] * (speedB - speed) * (load - loadA) +
                                                                 tableGear[(int)speedB][loadB] * (speed - speedA) * (load - loadA));
            if (double.IsNaN(gear))
                gear = 1;
            // Look up the closests RPM.
            var closestsSpeed = tableGear.Keys.OrderBy(x => Math.Abs(speed - x)).FirstOrDefault();
            var closestsLoad = tableGear[closestsSpeed].Keys.OrderBy(x => Math.Abs(x-load)).FirstOrDefault();
            
            //return new ShifterTableLookupResult((int)Math.Round(gear), closestsSpeed, closestsLoad);
            return new ShifterTableLookupResult(tableGear[closestsSpeed][closestsLoad], tableThrottle[closestsSpeed][closestsLoad], closestsSpeed, closestsLoad);
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