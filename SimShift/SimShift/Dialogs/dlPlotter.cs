using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Data;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlPlotter : Form
    {
        private ucPlotter plot;
        private Timer updater;

        public dlPlotter()
        {
            InitializeComponent();

            plot = new ucPlotter(5, new float[]
                                        {
                                            //1,
                                            //1,
                                            1,
                                            10,
                                            1000,
                                            2500,
                                            300
                                        });
            plot.Dock = DockStyle.Fill;
            Controls.Add(plot);

            Main.Data.DataReceived += Data_DataReceived;
        }

        private double prevSpeed;
        private double prevTime;

        private double acc;
        void Data_DataReceived(object sender, EventArgs e)
        {
            var miner = Main.Data.Active as Ets2DataMiner;
            var tel = Main.Data.Telemetry;// miner.MyTelemetry;

            if (tel.Time != prevTime) acc = (tel.Speed - prevSpeed)/(tel.Time - prevTime);
            var gameAcc =Main.Data.Active.Application == "eurotrucks2" && Main.Data.Active.SupportsCar
                              ? -miner.MyTelemetry.accelerationZ
                              : 0;
            var pwr = Main.Drivetrain.CalculatePower(tel.EngineRpm, Main.GetAxisOut(JoyControls.Throttle));
            var data = new double[]
                           {
                               Main.GetAxisOut(JoyControls.Throttle), 
                               //Main.GetAxisOut(JoyControls.Clutch), 
                               //0.5 - tel.gameSteer/2,
                               //gameAcc,
                               acc,
                               pwr - 1000,
                               tel.EngineRpm
                               - 2500,
                               tel.Speed*3.6
                           };

            plot.Add(data.ToList());
            prevSpeed = tel.Speed;
            prevTime = tel.Time;
        }
    }
}
