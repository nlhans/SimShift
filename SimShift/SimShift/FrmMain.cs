using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Data;
using SimShift.Dialogs;
using SimShift.Models;
using SimShift.Services;

namespace SimShift
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
            FormClosing += new FormClosingEventHandler(FrmMain_FormClosing);
                //FormClosed += new FormClosedEventHandler(FrmMain_FormClosed);
               /*d = new Ets2DataMiner();
d.Data += Data;*/
        }

        void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Main.Save();
        }
        /*
        private Ets2Engine eng = new Ets2Engine(3550);
        public Dictionary<float, List<double>> rpmPower100 = new Dictionary<float, List<double>>();
        public Dictionary<float, List<double>> rpmPower0 = new Dictionary<float, List<double>>(); 
        private Ets2DataMiner d;
        void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            string o = "rpm,acc 0%,acc 100%,POWAR 100%\r\n";

            var rpms = rpmPower100.Keys.Concat(rpmPower0.Keys).Distinct().ToList();
            rpms.Sort();

            foreach(float key in rpms)
            {
                var k100 = ((rpmPower100.ContainsKey(key)) ? GetMedian(rpmPower100[key]) : 0);
                var k0 = ((rpmPower0.ContainsKey(key)) ? GetMedian(rpmPower0[key]) : 0);

                o += key + "," + k0 + ","+k100+"\n";
            }

            File.WriteAllText("powers.csv", o);
        }
        public static double GetMedian(List<double> source)
        {
            double[] sourceNumbers = source.ToArray();
            //Framework 2.0 version of this method. there is an easier way in F4        
            if (sourceNumbers == null || sourceNumbers.Length == 0)
                return 0D;

            //make sure the list is sorted, but use a new array
            double[] sortedPNumbers = (double[])sourceNumbers.Clone();
            sourceNumbers.CopyTo(sortedPNumbers, 0);
            Array.Sort(sortedPNumbers);

            //get the median
            int size = sortedPNumbers.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
            return median;
        }

        public double Ets2CalcPower(double rpm, double throttle)
        {
            //
            var pwr = -387.2087 +
                      2.5141263*rpm -
                      0.002342*rpm*rpm +
                      0.00000115813418719154*rpm*rpm*rpm -
                      0.000000000288539083674621*rpm*rpm*rpm*rpm +
                      0.0000000000000231121026293405*rpm*rpm*rpm*rpm*rpm;
            return pwr;
        }

        private void Data(object sender, EventArgs eventArgs)
        {
            bool powerMeasurement = true;
            bool coastMeasurement = false;
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new EventHandler(Data), new object[2] { sender, eventArgs });
                    return;

                }

                label1.Text = " ";

                var spd = 25;
                var GearRatios = new double[12]
                             {
                                 11.73, 9.21, 7.09, 5.57, 4.35, 3.41, 2.7, 2.12, 1.63, 1.28, 1.0, 0.78
                             };
                for (int i = 0; i < 12; i++)
                    GearRatios[i] *= 3.4 * 18.3 / 3.6; // for every m/s , this much RPM's

                double powerRequired = 500;

                for (int i = 0; i <= 12; i++)
                {
                    var rpm = GearRatios[i]*spd;
                    if (rpm > 2500) continue;
                    if (rpm < 500) continue;

                    var thr = eng.CalculateThrottleByPower(rpm, powerRequired);
                    if (thr > 1) continue;

                    label1.Text += "G" + i + ": " + Math.Round(rpm) + "rpm / " + Math.Round(thr * 100, 1) + "% throttle / " + Math.Round(eng.CalculateFuelConsumption(rpm, thr), 1)+ "L/h\r\n";

                }

                label1.Text += "Gear: " + d.Telemetry.gear + "\r\nAcc: " + d.Telemetry.accelerationZ + "\r\nRPM: " + d.Telemetry.engineRpm + "\r\nSpeed: " + d.Telemetry.speed + "\r\nFuel:" + d.Telemetry.fuel + "\r\nFlow:" + d.FuelFlow + "\r\nThrottle:" + d.Telemetry.gameThrottle + "\r\nMax? FuelFlow:" + (d.FuelFlow / d.Telemetry.gameThrottle);
                //double expectedFuelFlow = 90.4802*Math.Exp(0.0009516195*d.Telemetry.engineRpm)*d.Telemetry.gameThrottle/7;
                //label1.Text += "\r\nE: " + expectedFuelFlow;
                double actualFuelFlow = d.FuelFlow / d.Telemetry.gameThrottle;
                if ((actualFuelFlow > 10000 || actualFuelFlow < 0) && !powerMeasurement)
                {
                    //
                }
                else
                {
                    if (d.NewFuelFlow || powerMeasurement)
                    {
                        int rpmindex = 0;

                        if (coastMeasurement)
                        {
                            double rpmSlope = 193.82136079395202687988053386429; //  2500 / (45 / 3.6);
                            double virtualRpm = rpmSlope*d.Telemetry.speed;
                            rpmindex = (int) virtualRpm/25;
                        }else
                        {
                            rpmindex = (int)d.Telemetry.engineRpm / 25;
                            
                        }
                        rpmindex *= 25;

                        if (d.Telemetry.gameThrottle >= 0.95)
                        {
                            if (rpmPower100.ContainsKey(rpmindex) == false)
                                rpmPower100.Add(rpmindex, new List<double>());

                            rpmPower100[rpmindex].Add((powerMeasurement) ? d.Telemetry.accelerationZ : actualFuelFlow);
                        }
                        else if (d.Telemetry.gameThrottle<0.1)
                        {
                            if (rpmPower0.ContainsKey(rpmindex) == false)
                                rpmPower0.Add(rpmindex, new List<double>());

                            rpmPower0[rpmindex].Add(d.Telemetry.accelerationZ);
                        }
                    }
                }
                
            }catch
            {
            }
        }
        */
        private void gamesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void generalSetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dlGearboxShifterTable shiftTable = new dlGearboxShifterTable();
            shiftTable.ShowDialog();
        }

        private void joystickToolStripMenuItem_Click(object sender, EventArgs e)
        {

            dlJoysticks joys = new dlJoysticks();
            joys.Show();
        }

        private void btServiceStartStop_Click(object sender, EventArgs e)
        {
            if(Main.Running)
                Main.Stop();else
            Main.Start();
        }
    }
}
