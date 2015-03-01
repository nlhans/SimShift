using System;
using System.Collections.Generic;
using System.Linq;
using SimShift.Utils;

namespace SimShift.Models
{
    public class Ets2Drivetrain : GenericDrivetrain
    {
        public double MaximumTorque { get; private set; }
        public double MaximumPower { get; private set; }

        private double Ets2Torque { get; set; }

        private int damagedGears;
        public override int Gears
        {
            get { return base.Gears - damagedGears; }
        }

        public override bool GotDamage(float damage)
        {
            var wasDamaged = damagedGears;
            damagedGears = (int) Math.Floor(damage*Gears);
            return wasDamaged != damagedGears;
        }

        //public double StallRpm { get; set; }
        //public double MaximumRpm { get; set; }

        /*
        public Ets2Drivetrain(string truck)
        {
            double torques = 1000.0;
            switch (truck)
            {
                case "Volvo":
                    torques = 3550;
                Gears = 12;
                GearRatios = new double[12]
                                 {
                                     11.73, 9.21, 7.09, 5.57, 4.35, 3.41, 2.7, 2.12, 1.63, 1.28, 1.0, 0.78
                                 };
                for (int i = 0; i < Gears; i++)
                    GearRatios[i] *= 3.4*18.3/3.6; // for every m/s , this much RPM's
                    break;

                case "Scania":
                    torques = 2700;// TODO: fix this
                Gears = 15;
                GearRatios = new double[15]
                                 {
                                     9.16, 7.33, 5.82, 4.66, 3.72, 3, 2.44, 1.96, 1.55, 1.24, 1, 0.8, 0.71, 0.65, 0.6
                                 };
                for (int i = 0; i < Gears; i++)
                    GearRatios[i] *= 2.8*18.3/3.6; // for every m/s , this much RPM's
                    break;

                case "Kenworth":
                    torques = 3550; // TODO:
                    GearRatios = new double[18]
                                 {
                                     14.89, 12.41, 10.4, 8.66, 7.32, 6.09, 5.05, 4.21, 3.54, 2.95, 2.47, 2.06, 1.74,
                                     1.45, 1.2, 1.00, 0.84, 0.70
                                 };

                    for (int i = 0; i < Gears; i++)
                        GearRatios[i] *= 3.36 * 18.3 / 3.6; // for every m/s , this much RPM's
                    break;
            }


            MaximumRpm = 2350;
            StallRpm = 900;
            // Everything depends on the number of torques.
            MaximumPower = torques / 3550 * 1260;
            MaximumTorque = torques * 4451 / 3550;
        }
        */
        public override  double CalculateTorqueN(double rpm)
        {
            var negativeTorqueNormalized = 1.7504 // 0
                                           - 7.0542/Math.Pow(10, 3)*rpm // 1
                                           + 9.1425/Math.Pow(10, 6)*rpm*rpm // 2
                                           - 4.1157/Math.Pow(10, 9)*rpm*rpm*rpm // 3
                                           + 0.6036/Math.Pow(10, 12)*rpm*rpm*rpm*rpm // 4
                ;// -0.2338 / Math.Pow(10, 15) * rpm * rpm * rpm * rpm * rpm; // 5

            // 906.051 was measured with Scania 730 hp engine.
            // This produces 3500Nm.
            // The brake torque is proportional to the ETS2 engine torque
            var negativeTorqueAbsolute = negativeTorqueNormalized*(906.051/3500.0*Ets2Torque);
            negativeTorqueAbsolute *= -1; // ;)
            return negativeTorqueAbsolute;
        }

        public override double CalculateTorqueP(double rpm, double throttle)
        {
            double negativeTorque = CalculateTorqueN(rpm);

            var positiveTorqueNormalized = -0.3789
                                           + rpm*0.0022716
                                           - rpm*rpm*0.0011134/1000
                                           + rpm*rpm*rpm*0.1372/1000000000;

            var positiveTorqueAbs = positiveTorqueNormalized*Ets2Torque;    

            return positiveTorqueAbs*throttle + negativeTorque*(1 - throttle);
        }

        public override double CalculatePower(double rpm, double throttle)
        {
            var torque = CalculateTorqueP(rpm, throttle);
            return torque * (rpm / 1000) / (1 / 0.1904) * 0.75f;

        }

        public override double CalculateThrottleByTorque(double rpm, double torque)
        {
            var negativeTorque = CalculateTorqueN(rpm);
            var positiveTorque = CalculateTorqueP(rpm, 1) - negativeTorque;

            return (torque - negativeTorque) / positiveTorque;
        }

        public override double CalculateFuelConsumption(double rpm, double throttle)
        {
            // Rough linearisation
            // 2100Nm=11.548
            // 3500Nm=14.94
            // from formula fuel=14.94*exp(0.0009*rpm)
            var amplitudeForEngine = 6.46 + 1/412.72*Ets2Torque;

            // This curve is not compensated for absolute values
            // The relative meaning is , however, correct, but comparisions between trucks is not possible!
            var amplitude = amplitudeForEngine * Math.Exp(rpm * 0.000918876); // consumption @ 100%
            throttle *= 100;
            var linearity = -8.0753*Math.Pow(throttle, 6)*Math.Pow(10, -12)
                            + 2.691*Math.Pow(throttle, 5)*Math.Pow(10, -9)
                            - 0.349616*Math.Pow(throttle, 4)*Math.Pow(10, -6)
                            + 23.577*Math.Pow(throttle, 3)*Math.Pow(10, -6)
                            - 0.918283*Math.Pow(10, -3)*Math.Pow(throttle, 2)
                            + 0.027293*throttle
                            + 0.0019368;
            
            var fuel = amplitude* linearity*1.22;
            fuel -= 0.25;
            if (fuel < 0) fuel = 0;
            return fuel;
        }

        #region Implementation of IConfigurable

        public override void ResetParameters()
        {

        }

        public override void ApplyParameter(IniValueObject obj)
        {
            base.ApplyParameter(obj);
            switch(obj.Key)
            {
                case "Ets2Engine":
                    Ets2Torque = obj.ReadAsFloat();
                    break;
            }
        }

        public override IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = base.ExportParameters().ToList();
            obj.Add(new IniValueObject(base.AcceptsConfigs, "Ets2Engine", Ets2Torque.ToString("0.0")));
            return obj;
        }

        #endregion
    }

}
