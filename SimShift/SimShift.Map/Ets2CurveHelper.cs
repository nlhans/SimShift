using System;

namespace SimShift.MapTool
{
    public class Ets2CurveHelper
    {
        public static double Hermite(float s, float P1, float P2, float T1, float T2)
        {
            double h1 = 2 * Math.Pow(s, 3) - 3 * Math.Pow(s, 2) + 1;          // calculate basis function 1
            double h2 = -2 * Math.Pow(s, 3) + 3 * Math.Pow(s, 2);              // calculate basis function 2
            double h3 = Math.Pow(s, 3) - 2 * Math.Pow(s, 2) + s;         // calculate basis function 3
            double h4 = Math.Pow(s, 3) - Math.Pow(s, 2);                   // calculate basis function 4


            return h1 * P1 +                    // multiply and sum all funtions
                       h2 * P2 +                    // together to build the interpolated
                       h3 * T1 +                    // point along the curve.
                       h4 * T2;

        }

        public static double HermiteTangent(float s, float P1, float P2, float T1, float T2)
        {
            double h1 = 6 * Math.Pow(s, 2) - 6*s;          // calculate basis function 1
            double h2 = -6 * Math.Pow(s, 2) + 6*s;              // calculate basis function 2
            double h3 = 3*Math.Pow(s, 2) - 4*s + 1;         // calculate basis function 3
            double h4 = 3*Math.Pow(s, 2) - 2*s;                   // calculate basis function 4


            return h1 * P1 +                    // multiply and sum all funtions
                       h2 * P2 +                    // together to build the interpolated
                       h3 * T1 +                    // point along the curve.
                       h4 * T2;
        }
    }
}