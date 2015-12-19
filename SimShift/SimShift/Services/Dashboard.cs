using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using AForge;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    class Dashboard : IControlChainObj
    {
        public IEnumerable<string> SimulatorsOnly { get { return new string[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new string[0]; } }
        public bool Enabled { get; private set; }
        public bool Active { get; private set; }
        public bool Requires(JoyControls c)
        {
            return false;
        }

        public double GetAxis(JoyControls c, double val)
        {
            return val;
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {

        }

        private SerialPort sp;
        private bool busy = false;
        public void TickTelemetry(IDataMiner data)
        {
            return;
            try
            {
                if (busy) return;
                busy = true;
                if (sp == null)
                {
                    sp = new SerialPort("COM4", 115200);
                    sp.Open();
                    WriteInitLedTable();
                }
                sp.Write("rpm " + Math.Round(data.Telemetry.EngineRpm) + "\r\n");
                sp.Write("gear " + Math.Round(data.Telemetry.Gear%7*1.0f) + "\r\n");
                busy = false;
            }
            catch(Exception ex)
            {
                
            }
        }


        public static Color GetRgb(double r, double g, double b)
        {
            return Color.FromArgb(255, (byte)(r * 255.0), (byte)(g * 255.0), (byte)(b * 255.0));
        }
        public static Color HsvToRgb(double h, double s, double v)
        {
            int hi = (int)Math.Floor(h / 60.0) % 6;
            double f = (h / 60.0) - Math.Floor(h / 60.0);

            double p = v * (1.0 - s);
            double q = v * (1.0 - (f * s));
            double t = v * (1.0 - ((1.0 - f) * s));

            Color ret;

            switch (hi)
            {
                case 0:
                    ret = Dashboard.GetRgb(v, t, p);
                    break;
                case 1:
                    ret = Dashboard.GetRgb(q, v, p);
                    break;
                case 2:
                    ret = Dashboard.GetRgb(p, v, t);
                    break;
                case 3:
                    ret = Dashboard.GetRgb(p, q, v);
                    break;
                case 4:
                    ret = Dashboard.GetRgb(t, p, v);
                    break;
                case 5:
                    ret = Dashboard.GetRgb(v, p, q);
                    break;
                default:
                    ret = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
                    break;
            }
            return ret;
        }
        private void WriteInitLedTable()
        {
            for (int led = 1; led <= 16; led++)
            {
                sp.Write("led-animation " + led + ",-1 2000,0,2000 200,1000\r\n");
                System.Threading.Thread.Sleep(10);
            }
            for (int gear = 0; gear < 10; gear++)
            {
                for (int led = 1; led <= 16; led++)
                {
                    Color c = Dashboard.HsvToRgb(led/16.0f*360, 1.0f, 1.0f);
                    var r = c.R * 10;
                    var g = c.G * 10;
                    var b = c.B*10;

                    var rpm = 800 + (1800 - 800) / 15.0f * (led-1);

                    var s = -150 + rpm;
                    var e = 150 + rpm;
                    if (s < 0) s = 0;
                    if (e > 2400) e = 2400;
                    sp.Write("led-animation "+led+","+gear+" "+r+","+g+","+b+" "+s+","+e+"\r\n");
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
    }
}
