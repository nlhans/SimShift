using System.Collections.Generic;

namespace SimShift.MapTool
{
    public class Ets2PrefabNode
    {
        public float X;
        public float Y;
        public float Z;

        public float RotationX;
        public float RotationY;
        public float RotationZ;

        public List<Ets2PrefabCurve> InputCurve;
        public List<Ets2PrefabCurve> OutputCurve;

        public double Yaw { get; set; }
        public int Node { get; set; }
    }
}