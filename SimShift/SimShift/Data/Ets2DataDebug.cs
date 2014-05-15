using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Services;

namespace SimShift.Data
{
    public partial class Ets2DataDebug : Form
    {
        private Timer updateTimer = new Timer();

        private uint lastTimestamp = uint.MinValue;
        private int timeoutTicker = 0;

        public Ets2DataDebug()
        {
            InitializeComponent();

            updateTimer.Tick += new EventHandler(updateTimer_Tick);
            updateTimer.Interval = 100;
            updateTimer.Start();
        }

        void updateTimer_Tick(object sender, EventArgs e)
        {
            //
            if (Main.Data.Active != null && Main.Data.Active.Application == "eurotrucks2" && !Main.Data.Active.SelectManually)
            {
                var ets2Miner = Main.Data.Active as Ets2DataMiner;
                var ets2Tel = ets2Miner.MyTelemetry;

                if  (Math.Abs(ets2Tel.time - lastTimestamp) < 100)
                {
                    timeoutTicker++;
                }
                else
                {
                    timeoutTicker = 0;
                }

                StringBuilder inftext = new StringBuilder();
                if (timeoutTicker >=3)
                {
                    inftext.AppendLine("Data stream paused");
                }else
                {
                    inftext.AppendLine("Data stream updating");
                }
                inftext.AppendLine("Game paused flag:" + (ets2Tel.paused == 1 ? "Yes" : "No"));
                inftext.AppendLine("Time: " + ets2Tel.time);
                inftext.AppendLine("Engine: " + (ets2Tel.engine_enabled ? "Running" : "Stalled"));
                inftext.AppendLine("Trailer: " + (ets2Tel.trailer_attached ? "Attached" : "Distached"));
                inftext.AppendLine("Truck ID: " + ets2Miner.Truck);
                inftext.AppendLine("Trailer ID: " + ets2Miner.Trailer);
                inftext.AppendLine("");
                inftext.AppendLine("Interpreted Trailer Information");
                inftext.AppendLine("");
                inftext.AppendLine("Trailer Name: " + ets2Miner.TrailerName);
                inftext.AppendLine("Trailer Tonnage: " + ets2Miner.TrailerTonnage);
                inftext.AppendLine("Plug-in reported truck weight: " + ets2Tel.truckWeight);
                inftext.AppendLine("Plug-in reported trailer weight: " + ets2Tel.trailerWeight);
                inftext.AppendLine("");
                inftext.AppendLine("Vehicle Dynamics");
                inftext.AppendLine("");
                inftext.AppendLine(string.Format("Acceleration: X{0:00.0000} / Y{1:00.0000} / Z{2:00.0000}",
                                                 ets2Tel.accelerationX, ets2Tel.accelerationY, ets2Tel.accelerationZ));
                inftext.AppendLine(string.Format("Coordinate: X{0:00.0000} / Y{1:00.0000} / Z{2:00.0000}",
                                                 ets2Tel.coordinateX, ets2Tel.coordinateY, ets2Tel.coordinateZ));
                inftext.AppendLine(string.Format("Rotation: X{0:00.0000} / Y{1:00.0000} / Z{2:00.0000}",
                                                 ets2Tel.rotationX, ets2Tel.rotationY, ets2Tel.rotationZ));
                inftext.AppendLine(string.Format("Speed: {0:00.00}m/s ({1:000.0}km/h / {2:000.0}mph)", ets2Tel.speed,ets2Tel.speed*3.6, ets2Tel.speed*3.6/1.6));


                inftext.AppendLine("");
                inftext.AppendLine("Vehicle Drivetrain");
                inftext.AppendLine("");
                inftext.AppendLine(string.Format("Engine RPM: {0:0000} / MAX {1:0000}", ets2Tel.engineRpm, ets2Tel.engineRpmMax));
                inftext.AppendLine(string.Format("Gear: {0:00} / MAX {1:00}", ets2Tel.gear, ets2Tel.gears));
                inftext.AppendLine(string.Format("Gear Range: {0:0} / MAX {1:0}", ets2Tel.gearRangeActive, ets2Tel.gearRanges));
                inftext.AppendLine(string.Format("Fuel: {0:0000.00}L / MAX {1:0000}", ets2Tel.fuel, ets2Tel.fuelCapacity));
                inftext.AppendLine(string.Format("Fuel Usage: {0:000.00}L/h / {1:000.00} km/l", ets2Tel.fuelCapacity, ets2Tel.fuelAvgConsumption));


                inftext.AppendLine("");
                inftext.AppendLine("Controls");
                inftext.AppendLine("");
                inftext.AppendLine(string.Format("User: {0:000.0}% T / {1:000.0}% B / {2:000.0}% C", ets2Tel.userThrottle * 100, ets2Tel.userBrake * 100, ets2Tel.userClutch * 100));
                inftext.AppendLine(string.Format("Game: {0:000.0}% T / {1:000.0}% B / {2:000.0}% C", ets2Tel.gameThrottle * 100, ets2Tel.gameBrake * 100, ets2Tel.gameClutch * 100));
                
                
                
                label1.Text = inftext.ToString();

                lastTimestamp = ets2Tel.time;
            }
            else
            {
                label1.Text = "Euro Truck Simulator 2 not running";
            }
        }
    }
}
