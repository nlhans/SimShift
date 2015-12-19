using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimShift.MapTool
{
    public class Ets2Node
    {
        public ulong NodeUID { get; private set; }

        public Ets2Item ForwardItem { get; set; }
        public ulong ForwardItemUID { get; private set; }
        public Ets2Item BackwardItem { get; set; }
        public ulong BackwardItemUID { get; private set; }

        public float X;
        public float Y;
        public float Z;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float Roll { get; private set; }

        public Ets2Point Point
        {
            get { return new Ets2Point(X, Y, Z, Yaw); }
        }

        public Ets2Node(byte[] stream, int position)
        {
            NodeUID = BitConverter.ToUInt64(stream, position);
            ForwardItemUID = BitConverter.ToUInt64(stream, position + 44);
            BackwardItemUID = BitConverter.ToUInt64(stream, position + 44 - 8);

            X = BitConverter.ToInt32(stream, position + 8) / 256.0f;
            Y = BitConverter.ToInt32(stream, position + 12) / 256.0f;
            Z = BitConverter.ToInt32(stream, position + 16) / 256.0f;

            var rX = BitConverter.ToSingle(stream, position + 20);
            var rY = BitConverter.ToSingle(stream, position + 24);
            var rZ = BitConverter.ToSingle(stream, position + 28);

            Yaw = (float) Math.PI-(float)Math.Atan2(rZ, rX);
            Yaw = Yaw%(float)Math.PI*2;
            // X,Y,Z is position of NodeUID
            // ForwardItemUID = Forward item
            // BackwardItemUID = Backward item

            //Console.WriteLine(position.ToString("X4") + " | " + NodeUID.ToString("X16") + " " + ForwardItemUID.ToString("X16") + " " + BackwardItemUID.ToString("X16") + " @ " + string.Format("{0} {1} {2} {3} {4} {5} {6} {7}de", X, Y, Z, rX,rY,rZ,Yaw,Yaw/Math.PI*180));

            Yaw = (float)Math.PI*0.5f - Yaw;
        }

        public override string ToString()
        {
            return "Node #" + NodeUID.ToString("X16") + " ("+X+","+Y+","+Z+")";
        }

        public IEnumerable<Ets2Item> GetItems()
        {
            if (ForwardItem != null)
                yield return ForwardItem;
            if (BackwardItem != null)
                yield return BackwardItem;
        }
    }
}
