using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimShift.Dialogs
{
    public partial class ucGearboxShifterGraph : UserControl
    {
        private List<double> simulatedLoads = new List<double>();
        private ShifterTableConfiguration config;

        public ucGearboxShifterGraph(ShifterTableConfiguration cfg)
        {
            config = cfg;
            InitializeComponent();
        }

        public void Update(List<double> loads)
        {
            simulatedLoads = loads;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.FillRectangle(Brushes.White, e.ClipRectangle);

            Func<float, float> translateX = (x) => 30.0f + (e.ClipRectangle.Width - 50.0f) * ((x) / 100.0f);
            Func<float, float> translateY = (y) => 30.0f + (e.ClipRectangle.Height - 50.0f)*(1-(y)/100.0f);

            var gridPen = new Pen(Color.LightSteelBlue, 1.0f);
            var linePens = new Pen[]
                               {
                                   new Pen(Color.Red, 1.0f),
                                   new Pen(Color.GreenYellow, 1.0f),
                                   new Pen(Color.DeepSkyBlue, 1.0f),
                                   new Pen(Color.Yellow, 1.0f),
                                   new Pen(Color.Pink, 1.0f),
                               };
            var linePens2 = new Pen[]
                               {
                                   new Pen(Color.DarkRed, 1.0f),
                                   new Pen(Color.DarkGreen, 1.0f),
                                   new Pen(Color.DarkBlue, 1.0f),
                                   new Pen(Color.Orange, 1.0f),
                                   new Pen(Color.Purple, 1.0f),
                               };

            for (int x = 0; x <= 100; x += 10)
                g.DrawLine(gridPen, translateX(x), translateY(0), translateX(x), translateY(100));
            for (int y = 0; y <= 100; y += 10)
                g.DrawLine(gridPen, translateX(0), translateY(y), translateX(100), translateY(y));

            var index = 0;
            foreach (var load in simulatedLoads)
            {
                double speed = 0.0f;
                int gear = 1;
                double time = 0.0;
                double time1 = 0.0;
                double shiftTimeout = 0.0;
                double shiftDeadtime = 0.0;
                double rpm = 0;
                float lastY = translateY(0);
                float lastY2 = translateY(0);
                try
                {
                    float maxTime = 120.0f;
                    float timeStep = 0.1f;
                    var throttle = 1;
                    for (time = 0; time < maxTime; time += timeStep)
                    {
                        if (gear < 0 || gear > 12)
                            break;

                        rpm = speed * config.Drivetrain.GearRatios[gear - 1] * 3.6;
                        var fuel = config.Drivetrain.CalculateFuelConsumption(rpm, load);
                        var power = config.Drivetrain.CalculateTorqueP(rpm<500?500:rpm, throttle*load/100.0) - config.Air.CalculateTorque(speed);
                        
                        if (shiftTimeout > time) power = 0;
                        double acceleration = power/2500;
                        if (rpm > 2500) acceleration = 0;

                        var bestGear = config.Lookup(speed*3.6, throttle*load/100.0);

                        if (speed > 80 / 3.6)
                            throttle = 0;

                        speed += timeStep * acceleration;

                        if (bestGear.Gear != gear && time > shiftDeadtime)
                        {
                            //Debug.WriteLine("[" + Math.Round(time, 2) + "] Shift to " + bestGear.Gear + " @ " + Math.Round(speed * 3.6, 1) + "kmh & " + Math.Round(rpm) + " to " + Math.Round(speed * config.GearRatios[bestGear.Gear - 1] * 3.6) + " rpm");
                            gear = bestGear.Gear;
                            shiftTimeout = time + 0.3;
                            shiftDeadtime = time + 0.1*gear;// +0.7;
                        }

                        if (Math.Abs(translateX((float)(time1 - time)) * 100 / maxTime) >= 5)
                        {
                            g.DrawLine(linePens[index],
                                       translateX((float) time1*100/maxTime), translateY(lastY*2),
                                       translateX((float) time*100/maxTime), translateY((float) speed*2));
                            g.DrawLine(linePens2[index],
                                       translateX((float) time1*100/maxTime), translateY(lastY2/25.0f),
                                       translateX((float) time*100/maxTime), translateY((float) rpm/25.0f));
                            time1 = time;
                        }
                        lastY = (float) speed;
                        lastY2 = (float) rpm;
                    }
                    Debug.WriteLine("Spd:" + speed);
                    Debug.WriteLine("RPM:" + rpm);
                    Debug.WriteLine("Gear:" + gear);
                }catch(Exception ab)
                {
                    
                }

                index++;
            }
        }

    }
}
