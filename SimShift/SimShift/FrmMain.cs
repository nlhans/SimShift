using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Dialogs;
using SimShift.Services;

namespace SimShift
{
    public partial class FrmMain : Form
    {
        private Timer updateModules;

        public FrmMain()
        {
            InitializeComponent();
            FormClosing += FrmMain_FormClosing;

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0,0);

            btServiceStartStop_Click(null, null);
            
            gbCarSelect.Enabled = false;

            updateModules = new Timer();
            updateModules.Interval = 25;
            updateModules.Tick += updateModules_Tick;
            updateModules.Start();
            
        }

        private Dictionary<string, string> loadedIcon = new Dictionary<string, string>(); 

        void updateModules_Tick(object sender, EventArgs e)
        {
            if (!Main.Running) return;
            var pane = gbModulesPane;

            var controlsChanged = false;
            var mods = Main.Controls.Chain;

            var throttleIn = Main.GetAxisIn(JoyControls.Throttle);
            var clutchIn = Main.GetAxisIn(JoyControls.Clutch);

            var throttleOut = 0.0;
            var clutchOut = 0.0;

            foreach(var mod in mods)
            {
                var name = mod.GetType().Name;
                if (mod.Enabled == false)
                {
                    if (pane.Controls.ContainsKey("name" + name))
                    {
                        pane.Controls.RemoveByKey("name" + name);
                        pane.Controls.RemoveByKey("pb" + name);
                        pane.Controls.RemoveByKey("thrAbs" + name);
                        pane.Controls.RemoveByKey("thrRel0" + name);
                        pane.Controls.RemoveByKey("thrRel1" + name);
                        pane.Controls.RemoveByKey("cltAbs" + name);
                        loadedIcon.Remove(name);
                        controlsChanged = true;
                    }
                    continue;
                }
                if (!pane.Controls.ContainsKey("name"+name))
                {
                    controlsChanged = true;

                    var lbl = new Label();
                    lbl.Text = name;
                    lbl.Size = new Size(160, 20);
                    lbl.Location = new Point(0, 0);
                    lbl.ForeColor = Color.White;
                    lbl.Font =new Font("Tahoma", 11.0f);
                    lbl.Name = "name" + name;

                    var pb = new PictureBox();
                    pb.Name = "pb" + name;
                    pb.Size = new Size(24, 24);
                    pb.Location = new Point(0, 0);
                    pb.BackColor = Color.Black;

                    var thrAbs = new PictureBox();
                    thrAbs.Name = "thrAbs" + name;
                    thrAbs.BackColor = Color.GreenYellow;

                    var thrRel0 = new PictureBox();
                    thrRel0.Name = "thrRel0" + name;
                    thrRel0.BackColor = Color.DarkRed;

                    var thrRel1 = new PictureBox();
                    thrRel1.Name = "thrRel1" + name;
                    thrRel1.BackColor = Color.DarkGreen;

                    var cltAbs = new PictureBox();
                    cltAbs.Name = "cltAbs" + name;
                    cltAbs.BackColor = Color.DeepSkyBlue;

                    pane.Controls.Add(pb);
                    pane.Controls.Add(lbl);

                    pane.Controls.Add(thrAbs);
                    pane.Controls.Add(thrRel0);
                    pane.Controls.Add(thrRel1);

                    pane.Controls.Add(cltAbs);

                    loadedIcon.Add(name, "");

                }else
                {

                    var pb = pane.Controls["pb" + name];

                    var iconFile = "Icons/" + name + ((mod.Active) ? "_active" : "") + ".png";
                    if (File.Exists(iconFile) && loadedIcon[name] != iconFile)
                    {
                        loadedIcon[name] = iconFile;
                        pb.BackgroundImage = Image.FromFile(iconFile);
                    }

                    throttleOut = Main.Controls.AxisProgression[JoyControls.Throttle][name];
                    clutchOut = Main.Controls.AxisProgression[JoyControls.Clutch][name];

                    // Display throttle
                    var thrAbs = pane.Controls["thrAbs" + name];
                    var thrRel0 = pane.Controls["thrRel0" + name];
                    var thrRel1 = pane.Controls["thrRel1" + name];

                    thrAbs.Size = new Size((int)(throttleOut * 100), 10);

                    var thrRelDbl = throttleOut-throttleIn;

                    if (double.IsNaN(thrRelDbl) || double.IsInfinity(thrRelDbl)) thrRelDbl = 0;
                    if (thrRelDbl < -1) thrRelDbl = -1;
                    if (thrRelDbl > 1) thrRelDbl = 1;

                    var thrRel0W = thrRelDbl > 0 ? 1 : (int)((0 - thrRelDbl)*50);
                    var thrRel1W = thrRelDbl < 0 ? 1 : (int)(thrRelDbl*50);

                    thrRel0.Location = new Point(250-thrRel0W, thrRel0.Location.Y);

                    thrRel0.Size = new Size(thrRel0W,10);
                    thrRel1.Size = new Size(thrRel1W, 10);

                    // Clutch
                    var cltAbs = pane.Controls["cltAbs" + name];

                    cltAbs.Size = new Size((int) (clutchOut*100), 10);

                    throttleIn = throttleOut;
                    clutchIn = clutchOut;
                }

                var l = pane.Controls["name" + name];

                if (typeof(CruiseControl) == mod.GetType())
                    l.Text = "CruiseControl " + Math.Round(3.6 * Main.CruiseControl.SpeedCruise);
            }

            if(controlsChanged)
            {
                var y = 5;
                foreach(var mod in mods)
                {
                    var name = mod.GetType().Name;
                    if (pane.Controls.ContainsKey("name" + name))
                    {
                        var lbl = pane.Controls["name" + name];
                        var pb = pane.Controls["pb" + name];
                        var thrAbs = pane.Controls["thrAbs" + name];
                        var thrRel0 = pane.Controls["thrRel0" + name];
                        var thrRel1 = pane.Controls["thrRel1" + name];
                        var cltAbs = pane.Controls["cltAbs" + name];
                        pb.Location = new Point(5, y);
                        lbl.Location = new Point(35, y + 4);
                        thrAbs.Location = new Point(200, y);
                        thrRel0.Location = new Point(200, y + 10);
                        thrRel1.Location = new Point(250, y + 10);
                        cltAbs.Location = new Point(310, y);
                        y += 26;
                    }
                }
            }
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

                this.cbSimList.DataSource = null;
                this.cbSimList.Items.Clear();
                this.cbSimList.DisplayMember = "Name";
                this.cbSimList.ValueMember = "Name";
                this.cbSimList.DataSource = Main.Data.Miners;

                    UpdateSimulatorStatusLabel();
                Main.Data.AppActive += new EventHandler(Data_AppActive);
                Main.Data.CarChanged += new EventHandler(Data_CarChanged);

                /*Dictionary<double, double> coastDat = new Dictionary<double, double>();
                Dictionary<double, double> powerDat = new Dictionary<double, double>();

                Application.ApplicationExit += (a,b) =>
                                                   {
                                                       var rpms =
                                                           coastDat.Keys.Concat(powerDat.Keys).Distinct().ToList();
                                                       rpms.Sort();
                                                       StringBuilder o = new StringBuilder();
                                                       foreach(var r in rpms)
                                                       {
                                                           o.Append(r + ", ");

                                                           if (coastDat.ContainsKey(r))
                                                               o.Append(Math.Round(coastDat[r], 6));
                                                           o.Append(", ");
                                                           if (powerDat.ContainsKey(r))
                                                               o .Append(Math.Round(powerDat[r], 6));
                                                           o.AppendLine(" ");
                                                           
                                                       }
                                                       File.WriteAllText("PowerCoastScaniaR.csv", o.ToString());
                                                   };
                Main.Data.DataReceived += (o, args) =>
                {
                    Debug.WriteLine(Main.Drivetrain.CalculateSpeedForRpm(8, 1000));
                                                  var tel = (Ets2DataMiner)(Main.Data.Active);
                                                  var ets2Tel = tel.MyTelemetry;
                    var targetgear = 8;
                    var r = ets2Tel.gear == targetgear ? ets2Tel.engineRpm :
                                                      Main.Drivetrain.CalculateRpmForSpeed(targetgear, ets2Tel.speed);

                                                  r = Math.Round(r/10)*10;
                                                  if (ets2Tel.gear == targetgear)
                                                  {
                                                      if (powerDat.ContainsKey(r))
                                                          powerDat[r] = powerDat[r] * 0.5 + ets2Tel.accelerationZ*0.5;
                                                      else
                                                          powerDat.Add(r, ets2Tel.accelerationZ);
                                                  }else if(ets2Tel.gear == 0)
                                                  {
                                                      if(coastDat.ContainsKey(r))
                                                      {
                                                          coastDat[r] = coastDat[r] * 0.5 + ets2Tel.accelerationZ * 0.5;
                                                      }else
                                                      {
                                                          coastDat.Add(r, ets2Tel.accelerationZ);
                                                      }
                                                  }
                                              };
*/
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
            if (Main.Transmission.Enabled)
            {
                Main.Transmission.Enabled = false;
                btTransmission.Text = "Manual Mode";
            }else
            {
                Main.Transmission.Enabled = true;
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

        private void plotterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var plotter = new dlPlotter();
            plotter.Show();
        }
    }
}
