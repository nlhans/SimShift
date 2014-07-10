using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Data.Common;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class ucDashboard : UserControl
    {
        private Color transparant = Color.Gray;
        private IDataDefinition data;
        private Bitmap needle;
        private double DrivenDistance;
        private double DrivenTime;
        private double DrivenFuel;

        public ucDashboard(Color t)
        {
            transparant = t;

            needle = (Bitmap)Image.FromFile(@"C:\Projects\Software\SimShift\Resources\needle_150px.png");

            this.DoubleClick+=new EventHandler(ucDashboard_DoubleClick);
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }
        private void ucDashboard_DoubleClick(object sender, EventArgs e)
        {
            DrivenDistance = 0;
            DrivenFuel = 0;
            DrivenTime = 0;
        }
        private Bitmap RotatePic(Bitmap bmpBU, float w, bool keepWholeImg)
        {
            Bitmap bmp = null;
            Graphics g = null;
            try
            {
                //Modus
                if (!keepWholeImg)
                {
                    bmp = new Bitmap(bmpBU.Width, bmpBU.Height);

                    g = Graphics.FromImage(bmp);
                    float hw = bmp.Width / 2f;
                    float hh = bmp.Height / 2f;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    //translate center
                    g.TranslateTransform(hw, hh);
                    //rotate
                    g.RotateTransform(w);
                    //re-translate
                    g.TranslateTransform(-hw, -hh);
                    g.DrawImage(bmpBU, 0, 0);
                    g.Dispose();
                }
                else
                {
                    //get the new size and create the blank bitmap
                    float rad = (float)(w / 180.0 * Math.PI);
                    double fW = Math.Abs((Math.Cos(rad) * bmpBU.Width)) + Math.Abs((Math.Sin(rad) * bmpBU.Height));
                    double fH = Math.Abs((Math.Sin(rad) * bmpBU.Width)) + Math.Abs((Math.Cos(rad) * bmpBU.Height));

                    bmp = new Bitmap((int)Math.Ceiling(fW), (int)Math.Ceiling(fH));

                    g = Graphics.FromImage(bmp);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    float hw = bmp.Width / 2f;
                    float hh = bmp.Height / 2f;

                    System.Drawing.Drawing2D.Matrix m = g.Transform;

                    //here we do not need to translate, we rotate at the specified point
                    m.RotateAt(w, new PointF((float)(bmp.Width / 2), (float)(bmp.Height / 2)), System.Drawing.Drawing2D.MatrixOrder.Append);

                    g.Transform = m;

                    //draw the rotated image
                    g.DrawImage(bmpBU, new PointF((float)((bmp.Width - bmpBU.Width) / 2), (float)((bmp.Height - bmpBU.Height) / 2)));
                    g.Dispose();
                }
            }
            catch
            {
                if ((bmp != null))
                {
                    bmp.Dispose();
                    bmp = null;
                }

                if ((g != null))
                {
                    g.Dispose();
                }

                MessageBox.Show("Fehler.");
            }

            return bmp;
        }

        private DateTime lastCalc = DateTime.Now;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var secondaryBackground = Color.Black;

            var g = e.Graphics;
            var r = e.ClipRectangle;

            g.FillRectangle(new SolidBrush(transparant), r);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            Point ptSpeedo = new Point(this.Width - this.Height-50, 50);
            Size szSpeedo = new Size(this.Height - 50, this.Height - 50);
            Point ptCenterSpeedo = new Point(ptSpeedo.X + szSpeedo.Width / 2, ptSpeedo.Y + szSpeedo.Height / 2);

            g.FillEllipse(new SolidBrush(secondaryBackground), new Rectangle(new Point(this.Width-this.Height-100, 0), new Size(this.Height+50, this.Height+50)));
            g.FillRectangle(new SolidBrush(secondaryBackground), this.Width-this.Height/2-60, 0, this.Height/2+60,this.Height );
            if (Main.Data == null) return;
            data = Main.Data.Telemetry;
            if (data == null) return;

            var speedoCircleStart = 30.0f;
            var speedoCircleEnd = -210.0f;
            var speedoCircleRange = speedoCircleStart - speedoCircleEnd;

            var speedoMin = 0;
            var speedoMax = Main.Data.Active.Application == "TestDrive2" ? 400 : 120;
            var speedoRange = speedoMax - speedoMin;
            var speedoTick = Main.Data.Active.Application == "TestDrive2" ? 50 : 5;
            var speedoTicks = speedoRange / speedoTick;

            var anglePerSpeedTick = speedoCircleRange / speedoTicks;

            var angleSpeedo = speedoCircleStart + speedoCircleRange * (data.Speed * 3.6 - speedoMin) / (speedoRange);
            angleSpeedo -= 270;
            angleSpeedo += speedoRange;
            var needleSpeedo = RotatePic(needle, (float)angleSpeedo, true);

            // Draw gauge
            Point ptCenterNeedle = new Point(ptSpeedo.X + szSpeedo.Width / 2 - needleSpeedo.Width / 2, ptSpeedo.Y + szSpeedo.Height / 2 - needleSpeedo.Height / 2);
            Rectangle rtSpeedo = new Rectangle(ptSpeedo, szSpeedo);
            Rectangle rtSpeedoInner = new Rectangle(new Point(ptSpeedo.X+3, ptSpeedo.Y+3), new Size(szSpeedo.Width-6, szSpeedo.Height-6));
            var radiusSpeedo = (float) szSpeedo.Width/2.0f;

            g.DrawArc(new Pen(Brushes.White, 3.0f), rtSpeedo, speedoCircleStart, -speedoCircleRange);
            g.DrawArc(new Pen(Brushes.White, 10.0f), rtSpeedo, speedoCircleStart, -speedoCircleRange);
            g.DrawArc(new Pen(Brushes.LightGray, 5.0f), rtSpeedoInner, speedoCircleStart, -speedoCircleRange);

            // Draw speedo
            var i = 0;
            for (var s = speedoCircleEnd; s <= speedoCircleStart; s += anglePerSpeedTick)
            {
                var radius1 = 0;
                var radius2 = radiusSpeedo;
                var radius3 = radiusSpeedo - 20;
                var a = s/360.0 * Math.PI * 2;
                var x1 = radius1 * Math.Cos(a) + ptCenterSpeedo.X;
                var y1 = radius1 * Math.Sin(a) + ptCenterSpeedo.Y;
                var x2 = radius2 * Math.Cos(a) + ptCenterSpeedo.X;
                var y2 = radius2 * Math.Sin(a) + ptCenterSpeedo.Y;
                var x3 = radius3 * Math.Cos(a) + ptCenterSpeedo.X - 10;
                var y3 = radius3 * Math.Sin(a) + ptCenterSpeedo.Y - 10;

                var speedo = speedoMin + speedoTick * i++;
                string speed = speedo.ToString();
                float fontSize, lineSize;
                if (speedo % 20 == 10)
                {
                    x3 += 4.0f;
                    fontSize = 7.0f;
                    lineSize = 3.0f;
                }
                else if (speedo % 20 == 5 || speedo % 20 == 15)
                {
                    lineSize = 1.0f;
                    fontSize = 5.0f;
                }else
                {
                    fontSize = 10.0f;
                    lineSize = 5.0f;
                }
                if (speed.Length == 3) x3 -= 7;

                g.DrawLine(new Pen(secondaryBackground, lineSize), (float)x1, (float)y1, (float)x2, (float)y2);
               
                
                // Choose not to draw 10s speed
                if (speedo % 20 != 0 && speedoTick == 5) continue;
                g.DrawString(speed, new Font("Verdana", fontSize, FontStyle.Bold), Brushes.WhiteSmoke, (float)x3, (float)y3);
            }

            // Draw Speedo needle

            double spdNeedleAngle = speedoCircleEnd - speedoCircleRange * (data.Speed*3.6 - speedoMin) / speedoRange;
            spdNeedleAngle += 60;
            spdNeedleAngle = spdNeedleAngle / -180 * Math.PI;
            var spdRad1 = radiusSpeedo;
            var spdRad2 = 0;

            var spdNeedlex1 = spdRad1 * Math.Cos(spdNeedleAngle) + ptCenterSpeedo.X;
            var spdNeedley1 = spdRad1 * Math.Sin(spdNeedleAngle) + ptCenterSpeedo.Y;
            var spdNeedlex2 = spdRad2 * Math.Cos(spdNeedleAngle) + ptCenterSpeedo.X;
            var spdNeedley2 = spdRad2 * Math.Sin(spdNeedleAngle) + ptCenterSpeedo.Y;

            g.DrawLine(new Pen(Brushes.Red, 3.0f), (float)spdNeedlex1, (float)spdNeedley1, (float)spdNeedlex2, (float)spdNeedley2);

            g.DrawImage(needleSpeedo, ptCenterNeedle);

            // Draw RPM


            var engineRpmMax = Main.Data.Active.Application == "TestDrive2" ? (float)Main.Drivetrain.MaximumRpm+1000 : 3000; ;
            var engineRpmMin = 0;
            var engineRpmRange = engineRpmMax - engineRpmMin;
            int engineRpmTick = Main.Data.Active.Application == "TestDrive2" ? 1000 : 250;
            var engineRpmTicks = engineRpmRange / engineRpmTick;

            var rpmCircleStart = -210.0f;
            var rpmCircleEnd = -110.0f;
            var rpmCircleRange = rpmCircleStart - rpmCircleEnd;

            var anglePerRpmTick = rpmCircleRange / engineRpmTicks;

            var rpmOverSize = 40;
            var ptRpm = new Point(ptSpeedo.X - rpmOverSize, ptSpeedo.Y - rpmOverSize);
            var szRpm = new Size(szSpeedo.Width + rpmOverSize*2, szSpeedo.Height + rpmOverSize*2);
            var rtRpm = new Rectangle(ptRpm, szRpm);
            var radiusRpmo = (float) szRpm.Width/2.0f;

            // Draw arc
            i = 0;
            var lastAngle = rpmCircleStart;
            var arcColor = Color.White;
            for (var s = rpmCircleStart; s <= rpmCircleEnd; s -= anglePerRpmTick)
            {
                var rpm = engineRpmMin + engineRpmTick * i;
                i++;
                if (Main.Data.Active.Application == "TestDrive2")
                {
                    if (rpm < Main.Drivetrain.StallRpm+1000)
                        arcColor = Color.Blue;
                    else if (rpm + 1000 > Main.Drivetrain.MaximumRpm)
                        arcColor = Color.Red;
                    else if (rpm > Main.Drivetrain.MaximumRpm)
                        arcColor = Color.DarkRed;
                    else
                    arcColor = Color.White;
                }
                else
                {
                    switch (rpm)
                    {
                        case 0:
                            arcColor = Color.Blue;
                            break;

                        case 750:
                            arcColor = Color.White;
                            break;

                        case 1250:
                            arcColor = Color.Green;
                            break;

                        case 1750:
                            arcColor = Color.WhiteSmoke;
                            break;

                        case 2250:
                            arcColor = Color.Red;
                            break;
                        case 3000:
                            arcColor = Color.DarkRed;
                            break;
                    }
                }
                g.DrawArc(new Pen(arcColor, 4.0f), rtRpm, lastAngle, -anglePerRpmTick);


                lastAngle = s;
            }

            // Draw labels+cutouts
            i = 0;
            for (var s = rpmCircleStart; s <= rpmCircleEnd; s -= anglePerRpmTick )
            {
                var rpm = engineRpmMin + engineRpmTick * i;
                i++;

                var a = s / 360.0 * Math.PI * 2;

                var radius1 = radiusRpmo+5;
                var radius2 = radiusRpmo - 5;
                
                var radius3 = radiusRpmo-15;

                var x1 = radius1 * Math.Cos(a) + ptCenterSpeedo.X;
                var y1 = radius1 * Math.Sin(a) + ptCenterSpeedo.Y;
                var x2 = radius2 * Math.Cos(a) + ptCenterSpeedo.X;
                var y2 = radius2 * Math.Sin(a) + ptCenterSpeedo.Y;

                var x3 = radius3 * Math.Cos(a) + ptCenterSpeedo.X - 8;
                var y3 = radius3 * Math.Sin(a) + ptCenterSpeedo.Y-8;
                if (rpm % 500 == 0)
                g.DrawString((rpm/100).ToString(), new Font("Verdana", 10.0f, FontStyle.Bold), Brushes.White, (float)x3, (float)y3);

                g.DrawLine(new Pen(secondaryBackground, (rpm % 500 == 0) ? 3.0f : 2.0f), (float)x1, (float)y1, (float)x2, (float)y2);
            }

            double rpmNeedleAngle = rpmCircleStart + rpmCircleRange*(data.EngineRpm - engineRpmMin)/engineRpmRange;
            rpmNeedleAngle +=60;
            rpmNeedleAngle = rpmNeedleAngle/-180*Math.PI;
            var rpmRad1 = radiusRpmo;
            var rpmRad2 = rpmRad1 - 25;

            var rpmNeedlex1 = rpmRad1 * Math.Cos(rpmNeedleAngle) + ptCenterSpeedo.X;
            var rpmNeedley1 = rpmRad1 * Math.Sin(rpmNeedleAngle) + ptCenterSpeedo.Y;
            var rpmNeedlex2 = rpmRad2 * Math.Cos(rpmNeedleAngle) + ptCenterSpeedo.X;
            var rpmNeedley2 = rpmRad2 * Math.Sin(rpmNeedleAngle) + ptCenterSpeedo.Y;

            g.DrawLine(new Pen(Brushes.Orange, 3.0f), (float)rpmNeedlex1, (float)rpmNeedley1, (float)rpmNeedlex2, (float)rpmNeedley2);


            // Draw power gauge

            var enginePwrMax = (float)Main.Drivetrain.CalculateMaxPower();
            var enginePwrMin = 0;
            var enginePwrRange = enginePwrMax - enginePwrMin;
            var enginePwrTick = (enginePwrMax/5);
            enginePwrTick = ((int) enginePwrTick/50)*50;
            var enginePwrTicks = enginePwrRange / enginePwrTick;

            var pwrCircleStart = 30.0f;
            var pwrCircleEnd = 130.0f;
            var pwrCircleRange = pwrCircleStart - pwrCircleEnd;

            var anglePerPwrTick = pwrCircleRange / enginePwrTicks;

            var pwrOverSize = 30;
            var ptpwr = new Point(ptSpeedo.X - pwrOverSize, ptSpeedo.Y - pwrOverSize);
            var szpwr = new Size(szSpeedo.Width + pwrOverSize * 2, szSpeedo.Height + pwrOverSize * 2);
            var rtpwr = new Rectangle(ptpwr, szpwr);
            var radiuspwro = (float)szpwr.Width / 2.0f;

            // Draw arc
            g.DrawArc(new Pen(Color.GreenYellow, 4.0f), rtpwr, pwrCircleStart, pwrCircleRange);

            // Draw labels+cutouts
            i = 0;
            for (var s = pwrCircleStart; s <= pwrCircleEnd; s -= anglePerPwrTick)
            {
                var pwr = enginePwrMin + enginePwrTick * i;
                i++;

                var a = (-s + 60) / 360.0 * Math.PI * 2;

                var radius1 = radiuspwro + 5;
                var radius2 = radiuspwro - 5;

                var radius3 = radiuspwro + 15;

                var x1 = radius1 * Math.Cos(a) + ptCenterSpeedo.X;
                var y1 = radius1 * Math.Sin(a) + ptCenterSpeedo.Y;
                var x2 = radius2 * Math.Cos(a) + ptCenterSpeedo.X;
                var y2 = radius2 * Math.Sin(a) + ptCenterSpeedo.Y;

                var x3 = radius3 * Math.Cos(a) + ptCenterSpeedo.X - 8;
                var y3 = radius3 * Math.Sin(a) + ptCenterSpeedo.Y - 8;

                if (pwr >= 200)
                    g.DrawString((pwr).ToString()+"hp", new Font("Verdana", 10.0f, FontStyle.Bold), Brushes.White, (float)x3, (float)y3);

                g.DrawLine(new Pen(secondaryBackground, (pwr % 500 == 0) ? 3.0f : 2.0f), (float)x1, (float)y1, (float)x2, (float)y2);
            }

            var myPwr = Main.Drivetrain.CalculatePower(data.EngineRpm, Main.GetAxisOut(JoyControls.Throttle));
            //if (myPwr < 0) myPwr = 0;
            double pwrNeedleAngle = pwrCircleStart - pwrCircleRange * (myPwr - enginePwrMin) / enginePwrRange;
            pwrNeedleAngle -= 60;
            pwrNeedleAngle = pwrNeedleAngle / -180 * Math.PI;
            var pwrRad1 = radiuspwro;
            var pwrRad2 = pwrRad1 - 25;

            var pwrNeedlex1 = pwrRad1 * Math.Cos(pwrNeedleAngle) + ptCenterSpeedo.X;
            var pwrNeedley1 = pwrRad1 * Math.Sin(pwrNeedleAngle) + ptCenterSpeedo.Y;
            var pwrNeedlex2 = pwrRad2 * Math.Cos(pwrNeedleAngle) + ptCenterSpeedo.X;
            var pwrNeedley2 = pwrRad2 * Math.Sin(pwrNeedleAngle) + ptCenterSpeedo.Y;

            g.DrawLine(new Pen(Brushes.Orange, 3.0f), (float)pwrNeedlex1, (float)pwrNeedley1, (float)pwrNeedlex2, (float)pwrNeedley2);

            // Gear
            var sGear = data.Gear.ToString();
            if (data.Gear == 0) sGear = "N";
            if (data.Gear == -1) sGear = "R";

            g.DrawString(sGear, new Font("Verdana", 14.0f), Brushes.White, ptCenterSpeedo.X-10, 10 );

            // Throttle
            var tWidth = Main.GetAxisOut(JoyControls.Throttle)*100;
            var bWidth = Main.GetAxisOut(JoyControls.Brake)*100;

            g.FillRectangle(new SolidBrush(Color.FromArgb(30,30,30)), ptCenterSpeedo.X-50, this.Height-30, 100, 20);
            g.FillRectangle(new SolidBrush(Color.DarkGreen), ptCenterSpeedo.X - 50, this.Height - 30, (float)tWidth, 10);
            g.FillRectangle(new SolidBrush(Color.DarkRed), ptCenterSpeedo.X - 50, this.Height - 20, (float)bWidth, 10);

            var literPerHour = Main.Drivetrain.CalculateFuelConsumption(data.EngineRpm, Main.GetAxisOut(JoyControls.Throttle));
            var kmPerHour = data.Speed*3.6;
            var kmPerLiter = kmPerHour/literPerHour;

            var literPer100KmInst = 100/kmPerLiter;
            if (literPer100Km < 0) literPer100Km = literPer100KmInst;
            else literPer100Km = literPer100KmInst;
            if (literPer100Km > 400) literPer100Km = 400;
            if (literPer100Km < 0) literPer100Km = 0;
            if (!double.IsNaN(literPer100Km) && !double.IsInfinity(literPer100Km))
                literPer100KmAvg = literPer100KmAvg*0.9995 + literPer100Km*0.0005;
            if (double.IsNaN(literPer100KmAvg) || double.IsInfinity(literPer100KmAvg))
                literPer100KmAvg = 0;

            g.DrawString(literPerHour.ToString("000.00 L/h"), new Font("Verdana", 8.0f), Brushes.DarkOrange, 0, 0);
            g.DrawString(string.Format("1:{0:00.000}km", kmPerLiter), new Font("Verdana", 8.0f), Brushes.DarkOrange, 0, 20);
            g.DrawString(string.Format("{0:00.000}l/100km", literPer100Km), new Font("Verdana", 8.0f), Brushes.DarkOrange, 0, 40);
            g.DrawString(string.Format("{0:00.000}l/100km", literPer100KmAvg), new Font("Verdana", 8.0f), Brushes.DarkOrange, 0, 60);

            g.DrawString(string.Format("{0:000.000}Nm", Main.Drivetrain.CalculateTorqueP(data.EngineRpm, data.Throttle)), new Font("Verdana", 8.0f), Brushes.DarkOrange, 0, 80);
            g.DrawString(string.Format("{0:000.000}Nm", Main.Drivetrain.CalculateTorqueN(data.EngineRpm)), new Font("Verdana", 8.0f), Brushes.DarkOrange, 0, 100);
            

            if (!Main.Data.Telemetry.Paused)
            {
                var scale = 3;
                var dt = DateTime.Now.Subtract(lastCalc).TotalMilliseconds/1000.0*scale;
                DrivenTime += dt;
                DrivenFuel += literPerHour*dt/3600.0;
                DrivenDistance += Math.Abs(data.Speed*dt);
            }
            if (double.IsNaN(DrivenTime) || double.IsInfinity(DrivenTime)) ucDashboard_DoubleClick(null, null);
            if (double.IsNaN(DrivenFuel) || double.IsInfinity(DrivenFuel)) ucDashboard_DoubleClick(null, null);
            if (double.IsNaN(DrivenDistance) || double.IsInfinity(DrivenDistance)) ucDashboard_DoubleClick(null, null);
            var tripStr = "Trip meter: " + (DrivenTime/60).ToString("000.0") + "m / " + (DrivenDistance/1000).ToString("000.00km") + " / " +
                          (DrivenFuel).ToString("000.00L") + "\r\n" +
                          (DrivenFuel/(DrivenDistance/100000)).ToString("000.00") + "l/100km / 1:" + (DrivenDistance/1000/DrivenFuel).ToString("0.00") + "km / " + (DrivenDistance/DrivenTime*3.6).ToString("000.00kmh");
            g.DrawString(tripStr, new Font("Verdana", 10.0f), Brushes.White, 80, 0);
            lastCalc = DateTime.Now;

            //g.DrawString(data.EngineRpm+"rpm", new Font("Arial", 10), Brushes.White, 10, 10 );

           // g.DrawString(myPwr + "HP", new Font("Arial", 10), Brushes.White, 10, 40);

        }

        private double literPer100KmAvg = 0;
        private double literPer100Km = -1;
    }
}
