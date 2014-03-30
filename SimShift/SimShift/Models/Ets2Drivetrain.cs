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
            double negativeTorque = -0.000638282 * rpm;

            negativeTorque /= 5.4542382267;
            negativeTorque *= MaximumTorque;

            return negativeTorque;
        }

        public override double CalculateTorqueP(double rpm, double throttle)
        {

            double positiveTorque = -7.14608640654085 + 0.0267618520312 * rpm
                - 0.0000222969198545134 * rpm * rpm
                + 0.00000000867342466120773 * rpm * rpm * rpm
                - 0.00000000000132793505659243 * rpm * rpm * rpm * rpm;
            positiveTorque /= 5.4542382267;
            positiveTorque *= MaximumTorque;

            double negativeTorque = CalculateTorqueN(rpm);

            if (rpm > MaximumRpm)
                return negativeTorque;
            else
            return positiveTorque * throttle + negativeTorque;
        }

        public override double CalculateThrottleByTorque(double rpm, double torque)
        {
            double positiveTorque = -7.14608640654085 + 0.0267618520312 * rpm
                - 0.0000222969198545134 * rpm * rpm
                + 0.00000000867342466120773 * rpm * rpm * rpm
                - 0.00000000000132793505659243 * rpm * rpm * rpm * rpm;
            positiveTorque /= 5.4542382267;
            positiveTorque *= MaximumTorque;

            double negativeTorque = CalculateTorqueN(rpm);

            return (torque - negativeTorque) / positiveTorque;
        }

        public override double CalculateFuelConsumption(double rpm, double throttle)
        {
            //
            double r = 95.7231762038 * Math.Exp(rpm * 0.000918876); // consumption @ 100%

            return r * throttle;
        }

        #region Implementation of IConfigurable

        public override void ResetParameters()
        {
            MaximumTorque = 2500;
        }

        public override void ApplyParameter(IniValueObject obj)
        {
            base.ApplyParameter(obj);
            switch(obj.Key)
            {
                case "Ets2Engine":
                    MaximumTorque = obj.ReadAsFloat();
                    break;
            }
        }

        public override IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = base.ExportParameters().ToList();
            obj.Add(new IniValueObject(base.AcceptsConfigs, "Ets2Engine", MaximumTorque.ToString("0.0")));
            return obj;
        }

        #endregion
    }

}
