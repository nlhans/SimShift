using System;

namespace SimShift.Models
{
    public class Ets2Engine
    {
        public double MaximumTorque { get; private set; }
        public double MaximumPower { get; private set; }

        public double StallRpm { get; private set; }
        public double MaximumRpm { get; private set; }

        public Ets2Engine(double torques)
        {
            MaximumRpm = 2500;
            StallRpm = 750;
            // Everything depends on the number of torques.
            MaximumPower = torques / 3550 * 1260;
            MaximumTorque = torques * 4451 / 3550;
        }

        public double CalculateTorqueN(double rpm)
        {
            double negativeTorque = -0.000638282 * rpm;

            negativeTorque /= 5.4542382267;
            negativeTorque *= MaximumTorque;

            return negativeTorque;
        }

        public double CalculateTorqueP(double rpm, double throttle)
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

        public double CalculateThrottleByTorque(double rpm, double torque)
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

        public double CalculatePower(double rpm, double throttle)
        {
            var torque = CalculateTorqueP(rpm, throttle);

            return torque*(rpm/1000)/(1/0.1904);
        }

        public double CalculateFuelConsumption(double rpm, double throttle)
        {
            //
            double r = 95.7231762038 * Math.Exp(rpm * 0.000918876); // consumption @ 100%

            return r * throttle;
        }

        public double CalculateThrottleByPower(double rpm, double powerRequired)
        {
            // 1 Nm @ 1000rpm = 0.1904hp
            // 1 Hp @ 1000rpm = 5.2521Nm
            if (rpm == 0) return 1;
            double torqueRequired = powerRequired / (rpm / 1000) * (1 / 0.1904);
            return CalculateThrottleByTorque(rpm, torqueRequired);
        }
    }

}
