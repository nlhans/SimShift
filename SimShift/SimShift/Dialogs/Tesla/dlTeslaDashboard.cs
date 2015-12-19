using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Entities;
using SimShift.Services;

namespace SimShift.Dialogs.Tesla
{
    public partial class dlTeslaDashboard : Form
    {
        private Image Back;

        private int tachoW, tachoH;
        private Image gps;


        public const string ResourceFolder = "C:/Projects/Software/SimShift/Resources/TeslaDash/";

        private float Speed = 1.0f;
        private float Power = 1.0f;
        private float RPM = 0.0f;

        public dlTeslaDashboard()
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            TransparencyKey = Color.FromArgb(15,16,2);
            Back = Bitmap.FromFile(ResourceFolder + "Background2.png");
            Back = ResizeImage(Back, 1400, 550);
            gps = new Bitmap(500, 500);

            var updateGps = new Timer();
            updateGps.Interval = 250;
            updateGps.Tick += (s, e) =>
            {
                var newInterval = updateGps_Tick(s, e);
                if (newInterval < 50)
                    newInterval = 50;
                if (newInterval > 1000)
                    newInterval = 1000;
                updateGps.Interval = newInterval;
            };
            updateGps.Start();

            var t = new Timer();
            t.Interval = 50;
            t.Tick += (s,e)=> Invalidate();
            t.Start();
        }

        private int updateGps_Tick(object sender, EventArgs e)
        {
            using (var g = Graphics.FromImage(gps))
            {
                return dlMap.RenderMap(new Rectangle(0, 0, gps.Width, gps.Height), g,false);
            }
        }

        PointF DistanceFromCenter(PointF center, double radius, double angle)
        {
            double angleInRadians = angle * Math.PI / 180;
            return new PointF((float)(center.X + radius * (Math.Cos(angleInRadians))),
                              (float)(center.Y + radius * (Math.Sin(angleInRadians))));
        }

        private List<float> powerHistory = new List<float>(); 
        private bool FillTelemetry()
        {
            if (Main.Data == null) return false;
            if (Main.Data.Telemetry == null) return false;

            var data = Main.Data.Telemetry;

            Speed = data.Speed*3.6f;
            RPM = data.EngineRpm;
            var kwPowerNow = Main.Drivetrain.CalculatePower(RPM, data.Throttle)*0.75f;

            powerHistory.Add((float)kwPowerNow);
            while (powerHistory.Count > 8)
                powerHistory.RemoveAt(0);

            Power = (int) (Power*0.7f+0.3f*powerHistory.Max());// peak detect

            return true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!FillTelemetry())
            {
                Speed = 0;
                RPM = 0;
                Power = 0;
            }

            Dictionary<float, float> speedLut = new Dictionary<float, float>();
            speedLut.Add(0, 119);
            speedLut.Add(20, 151);
            speedLut.Add(40, 181);
            speedLut.Add(60, 211);
            speedLut.Add(80, 241);
            speedLut.Add(90, 251);
            speedLut.Add(100, 257.5f);
            speedLut.Add(110, 262.875f);
            speedLut.Add(120, 268);
            speedLut.Add(125, 270);

            Dictionary<float, float> powerLut = new Dictionary<float, float>();
            powerLut.Add(0, 360);
            powerLut.Add(5, 351.6f);
            powerLut.Add(10, 348.5f);
            powerLut.Add(20, 345);
            powerLut.Add(30, 336.8f);
            powerLut.Add(35, 333.5f);
            powerLut.Add(40, 330);
            powerLut.Add(60, 321.7f);
            powerLut.Add(70, 318.4f);
            powerLut.Add(80, 315);
            powerLut.Add(120, 306.8f);
            powerLut.Add(140, 303.4f);
            powerLut.Add(160, 300);
            powerLut.Add(240, 291.85f);
            powerLut.Add(280, 288.19f);
            powerLut.Add(320, 285);
            powerLut.Add(400, 276.84f);
            powerLut.Add(480, 273.17f);
            powerLut.Add(640, 270);

            Dictionary<float, float> powerGLut = new Dictionary<float, float>();
            powerGLut.Add(0, 360);
            powerGLut.Add(15, 375);
            powerGLut.Add(30, 390);
            powerGLut.Add(60, 405);

            var shiftP = 0.0;
            if (Main.Transmission != null && Main.Transmission.configuration != null && Main.Transmission.configuration.Mode == ShifterTableConfigurationDefault.Economy)
                shiftP = (RPM - 1200)/300;
            else
                shiftP = (RPM - 1400)/300;
            if (shiftP < 0) shiftP = 0;
            if (shiftP > 1) shiftP = 1;
            shiftP = 1 - shiftP;

            var speedOuter = Color.FromArgb(255, 129, 255, 254).Blend(Color.FromArgb(255, 255, 30, 0), shiftP);
            var speedGlow = Color.FromArgb(255, 17, 36, 120).Blend(Color.FromArgb(255, 150, 10, 10), shiftP);
            var speedCenter = Color.FromArgb(0, 17, 36, 120).Blend(Color.FromArgb(0,0,0,0), shiftP);

            var powerOuter = Color.FromArgb(255, 255, 177, 8);
            var powerGlow = Color.FromArgb(255, 104, 46, 0);
            var powerCenter = Color.FromArgb(0, 104, 46, 0);

            var powerGOuter = Color.FromArgb(255, 102, 205, 50);
            var powerGGlow = Color.FromArgb(255, 40, 82, 20);
            var powerGCenter = Color.FromArgb(0, 40, 82, 20);

            using (var frame = new Bitmap(e.ClipRectangle.Width, e.ClipRectangle.Height))
            using (var g = Graphics.FromImage(frame))
            {
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.DrawImageUnscaled(gps, new Rectangle(0, Back.Height/2 - gps.Height/2, gps.Width, gps.Height));
                g.DrawImage(Back, e.ClipRectangle, new Rectangle(0, 0, Back.Width, Back.Height), GraphicsUnit.Pixel);

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                DrawGlowingNeedle(speedLut, Speed, speedOuter, speedGlow, speedCenter, g);

                if (Power >= 0)
                    DrawGlowingNeedle(powerLut, Power, powerOuter, powerGlow, powerCenter, g);
                else
                {
                    DrawGlowingNeedle(powerGLut, -Power, powerGOuter, powerGGlow, powerGCenter, g);
                }

                // Draw speed
                var eurostile86 = new Font("Eurostile-Normal", 60, FontStyle.Bold);
                var eurostile16 = new Font("Eurostile-Normal", 12.0f, FontStyle.Bold);

                var speedStr = ((int) Math.Round(Speed)).ToString();
                var speedX = 700 - 30*speedStr.Length;

                g.DrawString(speedStr, eurostile86, Brushes.Black, speedX + 5, 190 + 5);
                g.DrawString(speedStr, eurostile86, Brushes.White, speedX, 190);
                g.DrawString("km/h", eurostile16, Brushes.White, 680, 270);
                g.DrawString(Math.Round(Math.Abs(Power)).ToString("000") + "hp", eurostile16,
                    (Power > 0 ? Brushes.White : Brushes.GreenYellow), 670, 320);
                g.DrawString(Math.Round(RPM/1000.0f, 2).ToString("0.00") + "K rpm", eurostile16, Brushes.White, 660, 350);

                e.Graphics.DrawImageUnscaledAndClipped(frame, e.ClipRectangle);
            }
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void DrawGlowingNeedle(Dictionary<float, float> angleLut, float value, Color bOut, Color bGlow, Color bIn, Graphics g)
        {
            var angleStart = angleLut.Values.FirstOrDefault();

            var angleSweep = float.MaxValue;

            if (value < 0) value = 0;

            var lastK = 0.0f;
            var lastV = 0.0f;
            foreach (var kvp in angleLut)
            {
                if (kvp.Key >= value)
                {
                    var d = kvp.Key - lastK;
                    var s = kvp.Value - lastV;
                    var duty = (value - lastK)/d;

                    angleSweep = s*duty + lastV;
                    break;
                }
                else
                {
                    lastK = kvp.Key;
                    lastV = kvp.Value;
                }
            }
            if (float.IsNaN(angleSweep))
                angleSweep = angleStart;
            if (float.MaxValue == angleSweep)
                angleSweep = angleLut.Values.LastOrDefault();

            angleSweep -= angleStart;
            if (Math.Abs(angleSweep) < 0.1f) angleSweep = 0.1f;

            var angleEnd = angleStart + angleSweep;
            //angleSweep = 360;
            var finalRadius = 503/2 - 24/2; // outer radius of blue blob (-width of blob)
            var outerRadius = 456/2; // outer radius of glow
            var innerRadius = outerRadius - 30; // inner radius of glow
            var endRadius = 296/2; // radius of inner part
            innerRadius = 1;

            var dialX = 700;
            var dialY = 284;

            GraphicsPath path = new GraphicsPath();
            Point centerPoint = new Point(dialX, dialY);

            path.AddLine(this.DistanceFromCenter(centerPoint, innerRadius, angleStart),
                this.DistanceFromCenter(centerPoint, outerRadius, angleStart));
            path.AddArc(
                new RectangleF(centerPoint.X - (float) outerRadius, centerPoint.Y - (float) outerRadius, (float) outerRadius*2,
                    (float) outerRadius*2), angleStart, angleSweep);
            path.AddLine(this.DistanceFromCenter(centerPoint, outerRadius, angleEnd),
                this.DistanceFromCenter(centerPoint, innerRadius, angleEnd));
            path.AddArc(
                new RectangleF(centerPoint.X - (float) innerRadius, centerPoint.Y - (float) innerRadius, (float) innerRadius*2,
                    (float) innerRadius*2), angleEnd, -angleSweep);

            Blend blend = new Blend();
            // Create point and positions arrays
            float[] factArray = {0.1f, 1.0f, 1.0f};
            float[] posArray = {0.0f, 0.225f, 1.0f};
            // Set Factors and Positions properties of Blend
            blend.Factors = factArray;
            blend.Positions = posArray;

            PathGradientBrush pthGrBrush = new PathGradientBrush(path);
            pthGrBrush.Blend = blend;

            pthGrBrush.CenterColor = bIn;
            pthGrBrush.CenterPoint = centerPoint;

            Color[] colors = { bGlow, bGlow, bGlow, bGlow };
            pthGrBrush.SurroundColors = colors;

            g.FillPath(pthGrBrush, path);

            var p = new Pen(bOut, 22.0f);
            g.DrawArc(p, centerPoint.X - finalRadius, centerPoint.Y - finalRadius, finalRadius*2, finalRadius*2,
                angleStart, angleSweep);
            g.DrawLine(new Pen(bOut, 5.0f), this.DistanceFromCenter(centerPoint, finalRadius + 11, angleEnd),
                DistanceFromCenter(centerPoint, endRadius, angleEnd));
        }
    }
}
