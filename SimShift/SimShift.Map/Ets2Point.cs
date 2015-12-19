using System;
using System.Drawing;

namespace SimShift.MapTool
{
    public class Ets2Point
    {
        public float X;
        public float Y;
        public float Z;
        public float Heading;

        public Ets2Point(float x, float y, float z, float heading)
        {
            X = x;
            Y = y;
            Z = z;
            Heading = heading;
        }

        public Ets2Point(PointF fr)
        {
            X = fr.X;
            Y = 0;
            Z = fr.Y;
            Heading = 0;
        }

        public PointF ToPoint()
        {
            return new PointF(X, Z);
        }

        public override string ToString()
        {
            return "P " + Math.Round(X, 2) + "," + Math.Round(Z, 2) + " / " + Math.Round(Heading/Math.PI*180,1) + "deg (" +
                   Math.Round(Heading, 3) + ")";
        }

        public bool CloseTo(Ets2Point pt)
        {
            return DistanceTo(pt) <= 2f;
        }

        public float DistanceTo(Ets2Point pt)
        {
            if (pt == null) return float.MaxValue;
            var dx = pt.X - X;
            var dy = pt.Z - Z;

            var dst = (float) Math.Sqrt(dx * dx + dy * dy);

            return dst;
        }
    }
}