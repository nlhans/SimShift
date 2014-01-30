using System;

namespace SimShift.Models
{
    public class Ets2Aero
    {
        public double CalculateTorque(double speed)
        {
            var t = 3550.0 / 41964;

            var trq =t*0.0837217043 * Math.Exp(0.0002886324 * Math.Abs(speed * 200));
            trq *= 2*9000;
            return trq;
        }
    }
}