using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;
using AForge.Imaging;
using SimShift.Data.Common;
using SimShift.Utils;

namespace SimShift.Services
{
    public class LaneAssistance : IControlChainObj
    {
        public bool Enabled { get { return Active; } }
        public const bool UseDirectXCapture = false;

        public static Bitmap CameraInput;
        public static Bitmap CameraOutput;

        public bool Active { get; private set; }
        public double SteerAngle { get; private set; }
        public double LockedSteerAngle { get; private set; }

        public bool ButtonActive { get { return DateTime.Now > ButtonCooldownPeriod; } }

        private SoundPlayer beep = new SoundPlayer(@"C:\Projects\Software\SimShift\Resources\alert.wav");
        public DateTime ButtonCooldownPeriod = DateTime.Now;

        public DateTime LastStripeDetect = DateTime.Now;
        public bool LastStripeDetectInvalid { get { return DateTime.Now > LastStripeDetect; } }

        private Timer ScanMirrorTimer = new Timer();
        
        public LaneAssistance()
        {
            Main.Data.AppActive += new EventHandler(Data_AppActive);
            Main.Data.AppInactive += new EventHandler(Data_AppInactive);
            ets2Handle = IntPtr.Zero;

            ScanMirrorTimer.Interval = 50;
            ScanMirrorTimer.Tick += new EventHandler(ScanMirrorTimer_Tick);
            //ScanMirrorTimer.Start();

            CameraInput = new Bitmap(mirrorWidth*2, mirrorHeight);
            CameraOutput = new Bitmap(mirrorWidth * 2, mirrorHeight);
        }

        void ScanMirrorTimer_Tick(object sender, EventArgs e)
        {
            keepAlive++;
            if(Active|| keepAlive % 5 == 0)
            ScanMirrors();
        }

        void Data_AppInactive(object sender, EventArgs e)
        {
            ets2Handle = IntPtr.Zero;
        }

        void Data_AppActive(object sender, EventArgs e)
        {
            // euro truck simulator 2
            if (Main.Data.Active.Application.Contains("ruck"))
            {
                ets2Handle = Main.Data.Active.ActiveProcess.MainWindowHandle;
            }
        }

        #region Implementation of IControlChainObj


        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Steering:
                    return Active;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch(c)
            {
                case JoyControls.Steering:
                    return SteerAngle;

                default:
                    return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
            bool wasActive = Active;
            //
            if (Main.GetButtonIn(JoyControls.LaneAssistance) && ButtonActive)
            {
                Active = !Active;
                ButtonCooldownPeriod = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
                LastStripeDetect = DateTime.Now.Add(new TimeSpan(0, 0, 0, 500));
                driveOnRightMirror = false;
                driveOnLeftMirror = false;
                LockedSteerAngle = Main.GetAxisIn(JoyControls.Steering);
                Debug.WriteLine("Setting lane assistance to: " + Active);
            }

            if (Active && LastStripeDetectInvalid)
            {
                Active = false;
                Debug.WriteLine("[LA] Vision error");
            }

            if (Active && UseDirectXCapture && ets2Handle == IntPtr.Zero)
            {
                Active = false;
                Debug.WriteLine("[LA] No ETS2 handle");
            }

            // We can't do speeding with this mod..
            if (Active && Main.Data.Active.Telemetry.Speed > 110)
            {
                Active = false;
                Debug.WriteLine("[LA] User speeding >110kmh");
            }

            var currentSteerAngle = Main.GetAxisIn(JoyControls.Steering);

            // User overrides steering
            if (Active && Math.Abs(currentSteerAngle-LockedSteerAngle)>0.05)
            {
                Active = false;
                Debug.WriteLine("[LA] User override steering");
            }

            if (wasActive && !Active)
            {
                beep.Play();
            }
        }

        private int keepAlive = 0;
        public void TickTelemetry(IDataMiner data)
        {

            if (true)
            {
                if (validLeftMirror || validRightMirror)
                {
                    LastStripeDetect = DateTime.Now.Add(new TimeSpan(0, 0, 0, 500));
                }

                int inputValue = 0;

                if (validRightMirror && validLeftMirror)
                {
                    // Go for right, as we drive mostly on the right in Europe
                    inputValue = xRight;
                    driveOnRightMirror = true;
                    driveOnLeftMirror = false;
                }
                else if (validLeftMirror && !validRightMirror)
                {
                    inputValue = xLeft;
                    driveOnRightMirror = false;
                    driveOnLeftMirror = true;
                }
                else if (validRightMirror && !validLeftMirror)
                {
                    inputValue = xRight;

                    driveOnRightMirror = true;
                    driveOnLeftMirror = false;

                }
                else
                {
                    if (driveOnLeftMirror) inputValue = xLeft;
                    else if (driveOnRightMirror) inputValue = xRight;
                    else if (false)
                    {
                        Debug.WriteLine("[LA] Vision error; no stripe found");
                        Active = false;
                        beep.Play();
                    }
                }

                double lineDistanceError = inputValue - mirrorWidth / 2;
                //lineDistanceError *= -1;
                if (driveOnLeftMirror) lineDistanceError = 0 - lineDistanceError;
                var angleDistancError = aRight - 103;

                //dead zone 2 pixels
                if (Math.Abs(angleDistancError) > 30 && Active)
                {
                    beep.Play();
                }

                var totalSteerError = lineDistanceError/150.0 + angleDistancError/35;
                var steerErrorGain = 0.5 - 0.5*(data.Telemetry.Speed*3.6/40);
                if (steerErrorGain < 0.17) steerErrorGain = 0.17;
                SteerAngle = SteerAngle*0.2 + 0.8*(0.5 + totalSteerError*steerErrorGain);
                //Debug.WriteLine(lineDistanceError + "px error / " + angleDistancError + " angle error / " + SteerAngle);
            }
        }

        #endregion


        #region DataMining

        private IntPtr ets2Handle = IntPtr.Zero;

        public int xLeft = 0;
        public int xRight = 0;

        public float aLeft = 0;
        public float aRight = 0;

        public bool driveOnLeftMirror = false;
        public bool driveOnRightMirror = false;

        public bool validLeftMirror = false;
        public bool validRightMirror = false;

        public const int mirrorWidth = 108;
        public const int mirrorHeight = 50;
        public float brightness = 0;


        public void ScanMirrors()
        {
            validLeftMirror = false;
            validRightMirror = false;

            if (ets2Handle == IntPtr.Zero && UseDirectXCapture) return;
            
            try
            {

                var screenSrc = new Rectangle(0, 70 + 185, 1920, mirrorHeight);

                Bitmap screenBitmap;

                if (UseDirectXCapture) 
                    screenBitmap = Direct3DCapture.CaptureRegionDirect3D(ets2Handle, screenSrc);
                else
                {
                    screenBitmap = new Bitmap(screenSrc.Width, screenSrc.Height);
                    var srcG = Graphics.FromImage(screenBitmap);
                    srcG.CopyFromScreen(screenSrc.X, screenSrc.Y, 0, 0, new Size(screenSrc.Width, screenSrc.Height), CopyPixelOperation.SourceCopy);
                }

                var bL = new Bitmap(mirrorWidth, mirrorHeight);
                var bR = new Bitmap(mirrorWidth, mirrorHeight);
                var gL = Graphics.FromImage(bL);
                var gR = Graphics.FromImage(bR);

                //Copy mirros into 2 bitmaps
                gL.DrawImage(screenBitmap, 0, 0, new Rectangle(10, 0, mirrorWidth, mirrorHeight), GraphicsUnit.Pixel);
                gR.DrawImage(screenBitmap, 0, 0, new Rectangle(1805, 0, mirrorWidth, mirrorHeight), GraphicsUnit.Pixel);

                // Copy to camera in graphic
                var cameraInGraphics = Graphics.FromImage(CameraInput);
                cameraInGraphics.DrawImage(bL, 0, 0, bL.Width, bL.Height);
                cameraInGraphics.DrawImage(bR, bL.Width, 0, bR.Width, bR.Height);
                // Parse the bitmaps
                var sL = ParseMirror(bL, false);
                var sR = ParseMirror(bR, true);

                // Update
                bL = sL.image;
                bR = sR.image;

                if (sL.found)
                {
                    xLeft = sL.position;
                    aLeft = sL.angle;
                }
                if (sR.found)
                {
                    xRight = sR.position;
                    aRight = sR.angle;
                }

                validLeftMirror = sL.found;
                validRightMirror = sR.found;

                // Draw dots for tracking
                gL = Graphics.FromImage(bL);
                if (sL.position > 0)
                    gL.FillEllipse(Brushes.Red, sL.position, mirrorHeight - 5, 10, 10);
                gR = Graphics.FromImage(bR);
                if (sR.position > 0)
                    gR.FillEllipse(Brushes.Red, sR.position, mirrorHeight - 5, 10, 10);


                var cameraOutGraphics = Graphics.FromImage(CameraOutput);
                cameraOutGraphics.DrawImage(bL, 0, 0, bL.Width, bL.Height);
                cameraOutGraphics.DrawImage(bR, bL.Width, 0, bR.Width, bR.Height);

            }catch
            {
            }

        }
        public struct ScanResult
        {
            public bool found;
            public Bitmap image;
            public float brightness;
            public int position;
            public float angle;

            public ScanResult(bool found, Bitmap image, float brightness, int position, float angle)
            {
                this.found = found;
                this.image = image;
                this.brightness = brightness;
                this.position = position;
                this.angle = angle;
            }
        }

        private ScanResult ParseMirror(Bitmap b, bool mirrored)
        {
            var g = Graphics.FromImage(b);

            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.CompositingMode = CompositingMode.SourceCopy;

            // Normalize
            var pxVals = 0;
            // calculate average pixel lightyness
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    var px = b.GetPixel(x, y);

                    var grayNessValue = px.B + px.G + px.R;
                    pxVals += grayNessValue;
                }
            }

            pxVals /= b.Width;
            pxVals /= b.Height;
            pxVals /= 3;

            // offset required
            var offset = 110 - pxVals;
            double gain = 0.75 + 55.0 / pxVals;
            if (pxVals < 35 && false)
            {
                gain += 2 + (35 - pxVals) / 10.5;
                offset -= 30;
            }
            if (offset > 1)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    for (int y = 0; y < b.Height; y++)
                    {
                        var px = b.GetPixel(x, y);
                        var nR = px.R * gain + offset;
                        if (nR >= 255) nR = 255;
                        nR /= 2;
                        var nG = (px.G - 2) * gain + offset;
                        if (nG >= 255) nG = 255;
                        if (nG < 0) nG = 0;
                        var nB = (px.B - 4) * gain + offset;
                        if (nB >= 255) nB = 255;
                        if (nB < 0) nB = 0;
                        px = Color.FromArgb((int)nR, (int)nG, (int)nB);
                        b.SetPixel(x, y, px);
                    }
                }
            }

            float brightness = pxVals;

            b = AdjustBrightnessAndContrast(b, -1.5f, 4.5f, 1.5f);
            b = AdjustBrightnessAndContrast(b, 0.0f, 2.0f, 1.0f);

            BlobCounter bc = new BlobCounter();
            bc.MinWidth = 4;
            bc.MaxWidth = 15;
            bc.ObjectsOrder = ObjectsOrder.Size;
            bc.ProcessImage(b);
            var blobs = bc.GetObjectsInformation();

            g = Graphics.FromImage(b);
            var f = new Font("Arial", 8);
            Blob magicBlob = default(Blob);
            bool foundBlob = false;

            // Process each blob
            foreach (var blob in blobs)
            {
                Rectangle rect = blob.Rectangle;
                if (rect.Width > 4 && rect.Width < 90 && rect.Height > 7 && rect.Height > rect.Width - 5)
                {
                    g.DrawString(blob.ColorStdDev.R.ToString(), f, Brushes.White, 0, 0);
                    if (blob.ColorStdDev.R > 5 && blob.ColorStdDev.R < 60)
                        continue;
                    g.DrawRectangle(new Pen(Brushes.PaleGoldenrod), rect );
                    foundBlob = true;
                    magicBlob = blob;

                    break;

                }

            }

            int position = -1;
            float angle = 0;

            // We have found a line, possibly 
            if (foundBlob)
            {
                var lastDarkSpot = 0;
                var rect = magicBlob.Rectangle;
                for (int x = 0; x < magicBlob.Rectangle.Width; x++)
                {
                    var getX = rect.X + ((mirrored) ? x : (rect.Width - x));
                    var px1 = b.GetPixel(getX, rect.Y + rect.Height - 3);
                    var px2 = b.GetPixel(getX, rect.Y + rect.Height - 5);
                    var intensity = px1.G + px1.B + px1.R + px2.G + px2.B + px2.R;
                    intensity /= 2;
                    if (intensity < 50)
                    {
                        // Dark pixel
                        lastDarkSpot = x;
                    }
                    else
                    {
                        if (x - lastDarkSpot > 3)
                            break;
                    }
                }
                var angleRad = Math.Atan2(rect.Height, lastDarkSpot);
                angle = (float)(180 - angleRad / Math.PI * 180);
                position = (int) (rect.X + Math.Cos(angleRad)*(mirrorHeight-rect.Y));
                //position = lastDarkSpot + rect.X+20;

                // Project the position to the underside of the mirror
                // This is we come across dotted lines.
            }

            ScanResult sr = new ScanResult(foundBlob, b, brightness, position, angle);

            return sr;
        }
        private int GetRoadLine(Bitmap bitmap, int start, int end)
        {
            for (var y = bitmap.Height - 15; y > 0; y--)
            {
                var firstLinePixel = end;
                var linePixels = 0;
                var lastLinePixel = 0;
                for (var x = start; x < end; x++)
                {
                    var px = bitmap.GetPixel(x, y);
                    if ((px.B > 160 && px.G > 140 && px.R > 25 && Math.Abs(px.B - px.G) < 60) || // white
                        (px.R > 150 && px.G > 150 && Math.Abs(px.R - px.G) < 30)) // yello
                    {
                        firstLinePixel = Math.Min(x, firstLinePixel);
                        linePixels++;
                        if (x - lastLinePixel > 3)
                        {
                            // reset..
                            linePixels = 1;
                            firstLinePixel = end;
                        }
                        if (linePixels > 4 && x - firstLinePixel < 12) break;
                        lastLinePixel = x;

                    }


                }

                if (linePixels >= 4)
                {
                    return firstLinePixel;
                }
            }
            return -1;
        }
        private Bitmap AdjustBrightnessAndContrast(Bitmap originalImage, float brightness, float contrast, float gamma)
        {
            Bitmap adjustedImage = new Bitmap(originalImage.Width, originalImage.Height);

            float adjustedBrightness = brightness - 1.0f;
            // create matrix that will brighten and contrast the image
            float[][] ptsArray =
                {
                    new float[] {contrast, 0, 0, 0, 0}, // scale red
                    new float[] {0, contrast, 0, 0, 0}, // scale green
                    new float[] {0, 0, contrast, 0, 0}, // scale blue
                    new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
                    new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}
                };

            var imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            Graphics g = Graphics.FromImage(adjustedImage);
            g.DrawImage(originalImage, new Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height)
                        , 0, 0, originalImage.Width, originalImage.Height,
                        GraphicsUnit.Pixel, imageAttributes);

            return adjustedImage;
        }

        #endregion
    }
}
