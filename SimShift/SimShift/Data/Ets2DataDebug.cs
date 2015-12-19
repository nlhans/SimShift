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

                if  (Math.Abs(ets2Tel.Time - lastTimestamp) < 100)
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
                inftext.AppendLine("Game paused flag:" + (ets2Tel.Paused ? "Yes" : "No"));
                inftext.AppendLine("Time: " + ets2Tel.Time);
                inftext.AppendLine("Engine: " + (ets2Tel.Drivetrain.EngineEnabled ? "Running" : "Stalled"));
                inftext.AppendLine("Trailer: " + (ets2Tel.Job.TrailerAttached ? "Attached" : "Distached"));
                inftext.AppendLine("Truck ID: " + ets2Tel.TruckId);
                inftext.AppendLine("Trailer ID: " + ets2Tel.Job.TrailerId);
                inftext.AppendLine("");
                inftext.AppendLine("Interpreted Trailer Information");
                inftext.AppendLine("");
                inftext.AppendLine("Trailer Name: " + ets2Tel.Job.TrailerName);
                inftext.AppendLine("Trailer Tonnage: " + ets2Tel.Job.Mass);
                inftext.AppendLine("");
                inftext.AppendLine("Vehicle Dynamics");
                inftext.AppendLine("");
                inftext.AppendLine(string.Format("Acceleration: X{0:00.0000} / Y{1:00.0000} / Z{2:00.0000}",
                                                 ets2Tel.Physics.AccelerationX, ets2Tel.Physics.AccelerationY, ets2Tel.Physics.AccelerationZ));
                inftext.AppendLine(string.Format("Coordinate: X{0:00.0000} / Y{1:00.0000} / Z{2:00.0000}",
                                                 ets2Tel.Physics.CoordinateX, ets2Tel.Physics.CoordinateY, ets2Tel.Physics.CoordinateZ));
                inftext.AppendLine(string.Format("Rotation: X{0:00.0000} / Y{1:00.0000} / Z{2:00.0000}",
                                                 ets2Tel.Physics.RotationX, ets2Tel.Physics.RotationY, ets2Tel.Physics.RotationZ ));
                inftext.AppendLine(string.Format("Speed: {0:00.00}m/s ({1:000.0}km/h / {2:000.0}mph)", ets2Tel.Drivetrain.Speed,ets2Tel.Drivetrain.SpeedKmh, ets2Tel.Drivetrain.SpeedMph));


                inftext.AppendLine("");
                inftext.AppendLine("Vehicle Drivetrain");
                inftext.AppendLine("");
                inftext.AppendLine(string.Format("Engine RPM: {0:0000} / MAX {1:0000}", ets2Tel.Drivetrain.EngineRpm, ets2Tel.Drivetrain.EngineRpmMax));
                inftext.AppendLine(string.Format("Gear: {0:00} / MAX {1:00}", ets2Tel.Drivetrain.Gear, ets2Tel.Drivetrain.GearsForward));
                inftext.AppendLine(string.Format("Gear Range: {0:0} / MAX {1:0}", ets2Tel.Drivetrain.GearRange, ets2Tel.Drivetrain.GearRange));
                inftext.AppendLine(string.Format("Fuel: {0:0000.00}L / MAX {1:0000}", ets2Tel.Drivetrain.Fuel, ets2Tel.Drivetrain.FuelMax));
                inftext.AppendLine(string.Format("Fuel Usage: {0:000.00}L/h / {1:000.00} km/l", ets2Tel.Drivetrain.FuelRate, ets2Tel.Drivetrain.FuelAvgConsumption));


                inftext.AppendLine("");
                inftext.AppendLine("Controls");
                inftext.AppendLine("");
                inftext.AppendLine(string.Format("User: {0:000.0}% T / {1:000.0}% B / {2:000.0}% C", ets2Tel.Controls.UserThrottle * 100, ets2Tel.Controls.UserBrake * 100, ets2Tel.Controls.UserClutch* 100));
                inftext.AppendLine(string.Format("Game: {0:000.0}% T / {1:000.0}% B / {2:000.0}% C", ets2Tel.Controls.GameThrottle * 100, ets2Tel.Controls.GameBrake * 100, ets2Tel.Controls.GameClutch * 100));
                
                
                
                label1.Text = inftext.ToString();

                lastTimestamp = ets2Tel.Time;
            }
            else
            {
                label1.Text = "Euro Truck Simulator 2 not running";
            }
        }
    }
}
