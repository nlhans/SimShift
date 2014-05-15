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
using SimShift.Data.Common;
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
            FormClosing += FrmMain_FormClosing;

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0,0);

            btServiceStartStop_Click(null, null);
            
            gbCarSelect.Enabled = false;

        }

        void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Main.Save();
        }

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
            if (Main.Running)
            {
                Main.Data.AppActive -= new EventHandler(Data_AppActive);
                Main.Stop();
            }
            else
            {
                Main.Start();

                this.btSimMode.Text = "Auto";
                this.cbSimList.Items.Clear();
                this.cbSimList.DisplayMember = "Name";
                this.cbSimList.ValueMember = "Name";
                this.cbSimList.DataSource = Main.Data.Miners;

                    UpdateSimulatorStatusLabel();
                Main.Data.AppActive += new EventHandler(Data_AppActive);
                Main.Data.CarChanged += new EventHandler(Data_CarChanged);
                
            }
        }

        void CarProfile_LoadedProfile(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(CarProfile_LoadedProfile), new object[2] { sender, e });
                return;
            }
            if (!string.IsNullOrEmpty(Main.CarProfile.Active))
                lbProfiles.SelectedItem = Main.CarProfile.Active;
        }

        void Data_CarChanged(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(Data_CarChanged), new object[2] { sender, e });
                return;
            }
            var car = Main.Drivetrain.File;
            lblCars.Text = car;

            lbProfiles.Items.Clear();
            foreach (var profile in Main.CarProfile.Loaded)
                lbProfiles.Items.Add(profile.Name);
            if (!string.IsNullOrEmpty(Main.CarProfile.Active))
                lbProfiles.SelectedItem = Main.CarProfile.Active;

            Main.CarProfile.LoadedProfile += new EventHandler(CarProfile_LoadedProfile);
        }

        private delegate void voidDelegate();
        private void UpdateSimulatorStatusLabel()
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new voidDelegate(UpdateSimulatorStatusLabel), new object[0]);
                return;
            }
            lbSimStatus.Text = (Main.Data.AutoMode
                                   ? "Automatic Select"
                                   : "Manual Select") + 
                                   "\nSimulator: " +
                                     (Main.Data.Active != null ? Main.Data.Active.Name : "None");
        }

        private void btSimSelect_Click(object sender, EventArgs e)
        {
            if(Main.Running)
            {
                // Get the simulator from the miners list
                var miner = (IDataMiner)cbSimList.SelectedItem;
                if (miner == null) return;

                btSimMode.Text = "Manual";
                Main.Data.ManualSelectApp(miner);
                UpdateSimulatorStatusLabel();
            }
        }

        private void btCarApply_Click(object sender, EventArgs e)
        {
            Main.Data.ChangeCar((string)cbCars.SelectedItem);
        }

        private void btSimMode_Click(object sender, EventArgs e)
        {
            if (Main.Running)
            {
                if(Main.Data.AutoMode)
                {
                    Main.Data.ManualSelectApp(Main.Data.Active);
                    btSimMode.Text = "Manual";
                }else
                {
                    Main.Data.AutoSelectApp();
                    btSimMode.Text = "Auto";
                }
                UpdateSimulatorStatusLabel();
            }
        }

        void Data_AppActive(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(Data_AppActive), new object[2] {sender, e});
                return;
            }
            this.cbSimList.SelectedItem = Main.Data.Active.Name;
            UpdateSimulatorStatusLabel();
            this.gbCarSelect.Enabled = !Main.Data.Active.SupportsCar;

            // Select all drivetrains from this simulator
            List<string> myCars = new List<string>();
            
            var allCars = Directory.GetFiles("./Settings/Drivetrain/");
            foreach(var car in allCars)
            {
                
                var carCleaned = Path.GetFileNameWithoutExtension(car);

                if (carCleaned.StartsWith(Main.Data.Active.Application))
                {
                    var carWithoutApp = carCleaned.Substring(Main.Data.Active.Application.Length + 1);
                    myCars.Add(carWithoutApp);
                }
            }

            cbCars.Items.Clear();
            foreach (var c in myCars) cbCars.Items.Add(c);
        }

        private void laneAssistanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dlLaneAssistance la = new dlLaneAssistance();
            la.Show();
        }

        private void btTransmission_Click(object sender, EventArgs e)
        {
            if (Transmission.Enabled)
            {
                Transmission.Enabled = false;
                btTransmission.Text = "Manual Mode";
            }else
            {
                Transmission.Enabled = true;
                btTransmission.Text = "Auto Mode";
            }
        }

        private void dashboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dlDashboard dsh = new dlDashboard();
            dsh.Show();
        }

        private void euroTruckSimulator2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Ets2DataDebug ets2dbg = new Ets2DataDebug();
            ets2dbg.Show();
        }

        private void btClutch_Click(object sender, EventArgs e)
        {
            if (Main.Antistall == null) return;
            //Main.Antistall.Enabled = !Main.Antistall.Enabled;
            if (Main.Antistall.Enabled)
            {
                Main.Antistall.Enabled = false;
                btClutch.Text = "Manual Clutch";
            }
            else
            {
                Main.Antistall.Enabled = true;
                btClutch.Text = "Auto Clutch";
            }
        }
    }
}
