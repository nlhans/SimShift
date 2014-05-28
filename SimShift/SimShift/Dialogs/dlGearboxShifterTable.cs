using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SimShift.Models;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlGearboxShifterTable : Form
    {
        private int dataGridOverheadR = 0;
        private int dataGridOverheadB = 0;
        private int simGraphOverheadB = 0;

        // Given H,S,L in range of 0-1
        // Returns a Color (RGB struct) in range of 0-255
        public static Color HSL2RGB(double h, double sl, double l)
        {
            h = 1 - h;
            double v;
            double r, g, b;

            r = l; // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 4.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            Color rgb = Color.FromArgb(Convert.ToByte(r * 255.0f),Convert.ToByte(g * 255.0f), Convert.ToByte(b * 255.0f));
            return rgb;
        }

        private ShifterTableConfiguration activeConfiguration = new ShifterTableConfiguration(ShifterTableConfigurationDefault.Efficiency, new Ets2Drivetrain(), 5);

        public dlGearboxShifterTable()
        {
            var myEngine = new Ets2Drivetrain();
            Main.Load(myEngine, "Settings/Drivetrain/eurotrucks2.scania.r.ini");
            activeConfiguration = new ShifterTableConfiguration(ShifterTableConfigurationDefault.Henk, myEngine, 5);

            string headline = "RPM";
            for (int k = 0; k <= 10; k++)
                headline = headline +  ",Ratio " + k;
            //",Fuel " + k + ",Power " + k +
            headline = headline + "\r\n";

            List<string> fuelStats = new List<string>();
                for(float rpm = 0; rpm < 2500; rpm+=100)
                {
                    string l = rpm + "";
                    for (int load = 0; load <= 10; load++)
                    {
                        float throttle = load/20.0f;
                        var fuelConsumption = activeConfiguration.Drivetrain.CalculateFuelConsumption(rpm, throttle);
                        var power = activeConfiguration.Drivetrain.CalculatePower(rpm, throttle);
                        var fuel2 = (power / fuelConsumption) / rpm;
                        //"," + fuelConsumption + "," + power + 
                        l = l + "," +fuel2;
                    }

                    fuelStats.Add(l);
                }

            File.WriteAllText("./fuelstats.csv", headline+ string.Join("\r\n", fuelStats));

            // 
                // sim
                // 
                this.sim = new ucGearboxShifterGraph(this.activeConfiguration);
            this.sim.Location = new System.Drawing.Point(12, 283);
            this.sim.Name = "sim";
            this.sim.Size = new System.Drawing.Size(854, 224);
            this.sim.TabIndex = 2;
            this.Controls.Add(this.sim);

            SizeChanged += new EventHandler(dlGearboxShifterTable_SizeChanged);


            InitializeComponent();
            LoadTable();

            shifterTable.SelectionChanged += new EventHandler(shifterTable_SelectionChanged);
            dataGridOverheadR = this.Width - this.shifterTable.Width;
            dataGridOverheadB = this.sim.Location.Y - shifterTable.Location.Y - shifterTable.Height;
            simGraphOverheadB = this.Height - this.sim.Height;
        }

        void shifterTable_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                // Get which rows are selected.
                int sel = shifterTable.SelectedCells.Count;
                List<double> loads = new List<double>();
                for (int i = 0; i < sel; i++)
                    loads.Add(double.Parse(shifterTable.SelectedCells[i].Value.ToString()));
                sim.Update(loads);
            }
            catch(Exception aad)
            {
                
            }
        }

        void dlGearboxShifterTable_SizeChanged(object sender, EventArgs e)
        {
            this.sim.Location = new Point(sim.Location.X, this.Height-simGraphOverheadB);
            this.sim.Size = new Size(this.Width - dataGridOverheadR, sim.Height);
            this.shifterTable.Size = new Size(this.Width - dataGridOverheadR, sim.Location.Y - shifterTable.Location.Y - dataGridOverheadB);
        }

        private void LoadTable()
        {
            shifterTable.Rows.Clear();
            shifterTable.Columns.Clear();

            shifterTable.Columns.Add("Load", "Load");

            List<Color> gearColors = new List<Color>();
            gearColors.Add(Color.White);
            for (int gear = 0; gear < activeConfiguration.Drivetrain.Gears; gear++)
                gearColors.Add(HSL2RGB(gear / 1.0 / activeConfiguration.Drivetrain.Gears, 0.5, 0.5));

            var spdBins = 0;
            var spdBinsData = new List<double>();
            foreach (var spd in activeConfiguration.table.Keys)
            {
                if (spd % 3 == 0)
                {
                    shifterTable.Columns.Add(spd.ToString(), spd.ToString());

                    spdBinsData.Add(spd);
                    spdBins++;
                }
            }
            foreach (var load in activeConfiguration.table[0].Keys)
            {
                var data = new object[spdBins + 1];
                data[0] = Math.Round(load*100).ToString();
                for(int i =0 ; i < spdBins;i++)
                    data[i + 1] = activeConfiguration.Lookup(spdBinsData[i], load).Gear;
                shifterTable.Rows.Add(data);
            }
            for (int col = 0; col < shifterTable.Columns.Count; col++)
                shifterTable.Columns[col].Width = 33;
            for (int row = 0; row < shifterTable.Rows.Count; row++)
            {
                for(int spd = 1; spd <= spdBins; spd++)
                {
                    if (shifterTable.Rows[row].Cells[spd].Value != null)
                    shifterTable.Rows[row].Cells[spd].Style.BackColor = gearColors[int.Parse(shifterTable.Rows[row].Cells[spd].Value.ToString())];
                }
            }
        }
    }
}
