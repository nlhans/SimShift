using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Data;
using SimShift.Entities;
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
                                            1,
                                            2500,
                                            300,
                                            800
                                        });
            plot.Dock = DockStyle.Fill;
            Controls.Add(plot);

            Main.Data.DataReceived += Data_DataReceived;
            var tmr = new Timer {Enabled = true, Interval = 1000};
            tmr.Tick += tmr_Tick;
            tmr.Start();
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            plot.Frequency = hz;
            hz = 0;
        }

        private double prevSpeed;
        private double prevTime;

        private double acc;
        private int hz = 0;
        private void Data_DataReceived(object sender, EventArgs e)
        {
            var miner = Main.Data.Active as Ets2DataMiner;
            var tel = Main.Data.Telemetry; // miner.MyTelemetry;

            if (tel.Time - prevTime > 0.0001)
            {
                hz++;
                var data = new double[]
                           {
                               Main.GetAxisOut(JoyControls.Throttle),
                               Main.GetAxisOut(JoyControls.Clutch),
                               tel.EngineRpm - 2500,
                               tel.Speed*3.6,
                               VariableSpeedTransmission.reqpower
                           };

                plot.Add(data.ToList());
            }
            prevSpeed = tel.Speed;
            prevTime = tel.Time;

        }
    }
}
