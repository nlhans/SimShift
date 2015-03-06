using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimShift.Dialogs;
using SimShift.Entities;
using SimShift.Models;
using SimShift.Services;

namespace SimShift.Simulation
{
    public class SimulationEnvironment
    {
        private IDrivetrain drivetrain;
        private ShifterTableConfiguration shifter;


        private double Speed = 0.0f;

        public SimulationEnvironment()
        {
            drivetrain = new Ets2Drivetrain();
            Main.Load(drivetrain, "Settings/Drivetrain/eurotrucks2.scania.g7ld6x2.ini");
            shifter = new ShifterTableConfiguration(ShifterTableConfigurationDefault.Performance, drivetrain, 1, 0);

            Speed = 30/3.6;
            StringBuilder sim = new StringBuilder();
            for (int k = 0; k < 10000; k++)
            {
                Tick();
                sim.AppendLine(k + "," + Speed);
            }

            File.WriteAllText("./sim.csv", sim.ToString());
        }

        public void Tick()
        {
            // Model : engine
            var topGear = drivetrain.Gears - 1;
            var engineRpm = drivetrain.CalculateRpmForSpeed(topGear, (float)Speed);
            var enginePower = drivetrain.CalculatePower(engineRpm, 1.0f);

            // Model: aero
            var aero = Speed*Speed*0.5;

            var acceleration = enginePower - aero;
            acceleration /= 100;
            Speed += acceleration*1.0/100;

        }
    }
}
