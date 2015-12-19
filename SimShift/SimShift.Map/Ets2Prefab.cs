using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;

namespace SimShift.MapTool
{
    public class Ets2Prefab
    {
        private Ets2Mapper map;
        public string FilePath { get; private set; }
        public Ets2Company Company { get; set; }

        private byte[] Stream;

        public int IDX;
        public string IDSII;

        public List<Ets2PrefabNode> Nodes = new List<Ets2PrefabNode>();
        public List<Ets2PrefabCurve> Curves = new List<Ets2PrefabCurve>();

        public Ets2Prefab(Ets2Mapper mapper, string file)
        {
            map = mapper;
            FilePath = file;

            if (File.Exists(file))
            {
                Stream = File.ReadAllBytes(file);
                Parse();
            }
        }

        private void Parse()
        {
            var version = BitConverter.ToInt32(Stream, 0);

            var verbose = 0;
            if (Path.GetFileNameWithoutExtension(FilePath) == "road1_x_road1_t" ||
                Path.GetFileNameWithoutExtension(FilePath) == "hw3-2_x_hw2-2_full  ")
            {
                verbose++;
            }
            if (version != 21) // euhm..
            {

            }

            var nodes = BitConverter.ToInt32(Stream, 4);
            var terrain = BitConverter.ToInt32(Stream, 12);
            var navCurves = BitConverter.ToInt32(Stream, 8);
            var signs = BitConverter.ToInt32(Stream, 16);
            var spawns = BitConverter.ToInt32(Stream, 20);
            var semaphores = BitConverter.ToInt32(Stream, 24);
            var mappoints = BitConverter.ToInt32(Stream, 28);
            var triggers = BitConverter.ToInt32(Stream, 32);
            var intersections = BitConverter.ToInt32(Stream, 36);

            var nodeOffset = BitConverter.ToInt32(Stream, 44);
            var off2 = BitConverter.ToInt32(Stream, 48);
            var off3 = BitConverter.ToInt32(Stream, 52);
            var off4 = BitConverter.ToInt32(Stream, 56);

            //if (verbose == 0) return;

            for (int navCurve = 0; navCurve < navCurves; navCurve++)
            {
                var curveOff = off2 + navCurve*128;

                // 76: next curve
                // 92: prev curve
                List<int> nextCurve = new List<int>();
                List<int> prevCurve = new List<int>();
                for (int k = 0; k < 4; k++)
                {
                    nextCurve.Add(BitConverter.ToInt32(Stream, 76 + k*4 + curveOff));
                    prevCurve.Add(BitConverter.ToInt32(Stream, 92 + k*4 + curveOff));
                }

                var curve = new Ets2PrefabCurve
                {
                    Index = navCurve,

                    StartX = BitConverter.ToSingle(Stream, 16 + curveOff),
                    StartY = BitConverter.ToSingle(Stream, 20 + curveOff),
                    StartZ = BitConverter.ToSingle(Stream, 24 + curveOff),
                    EndX = BitConverter.ToSingle(Stream, 28 + curveOff),
                    EndY = BitConverter.ToSingle(Stream, 32 + curveOff),
                    EndZ = BitConverter.ToSingle(Stream, 36 + curveOff),
                    StartRotationX = BitConverter.ToSingle(Stream, 40 + curveOff),
                    StartRotationY = BitConverter.ToSingle(Stream, 44 + curveOff),
                    StartRotationZ = BitConverter.ToSingle(Stream, 48 + curveOff),
                    EndRotationX = BitConverter.ToSingle(Stream, 52 + curveOff),
                    EndRotationY = BitConverter.ToSingle(Stream, 56 + curveOff),
                    EndRotationZ = BitConverter.ToSingle(Stream, 60 + curveOff),

                    StartYaw = 
                        Math.Atan2(BitConverter.ToSingle(Stream, 48 + curveOff),
                            BitConverter.ToSingle(Stream, 40 + curveOff)),
                    EndYaw = 
                        Math.Atan2(BitConverter.ToSingle(Stream, 60 + curveOff),
                            BitConverter.ToSingle(Stream, 52 + curveOff)),

                    Length = BitConverter.ToSingle(Stream, 72 + curveOff),

                    Next = nextCurve.Where(i => i != -1).ToArray(),
                    Prev = prevCurve.Where(i => i != -1).ToArray()
                };

                Curves.Add(curve);

                
                var nextC = string.Join(",", nextCurve.Where(i => i >= 0));
                var prevC = string.Join(",", prevCurve.Where(i => i >= 0));
                //Console.WriteLine("UID: " + BitConverter.ToUInt64(Stream,curveOff).ToString("X16") + " Curve " + navCurve + " NEXT: " +nextC+ " PREV: " + prevC);
                for (int k = 0; k < 16; k += 4)
                {
                    var i32 = BitConverter.ToInt32(Stream, k + curveOff);
                    var u64 = BitConverter.ToUInt64(Stream, k + curveOff);
                    var f32 = BitConverter.ToSingle(Stream, k + curveOff);
                    //Console.WriteLine(k + " : INT32: " + i32 + " UINT64: " + u64.ToString("X16") + " FLOAT: " + f32);
                }/* for (int k = 64; k < 72; k += 4)
                {
                    var i32 = BitConverter.ToInt32(Stream, k + curveOff);
                    var u64 = BitConverter.ToUInt64(Stream, k + curveOff);
                    var f32 = BitConverter.ToSingle(Stream, k + curveOff);
                    Console.WriteLine(k + " : INT32: " + i32 + " UINT64: " + u64.ToString("X16") + " FLOAT: " + f32);
                } for (int k = 108; k < 128; k += 4)
                {
                    var i32 = BitConverter.ToInt32(Stream, k + curveOff);
                    var u64 = BitConverter.ToUInt64(Stream, k + curveOff);
                    var f32 = BitConverter.ToSingle(Stream, k + curveOff);
                    Console.WriteLine(k + " : INT32: " + i32 + " UINT64: " + u64.ToString("X16") + " FLOAT: " + f32);
                }*/
                
            }

            for (int navCurve = 0; navCurve < navCurves; navCurve++)
            {
                Curves[navCurve].NextCurve = Curves[navCurve].Next.Select(x => Curves[x]).ToList();
                Curves[navCurve].PrevCurve = Curves[navCurve].Prev.Select(x => Curves[x]).ToList();
            }

            for (int node = 0; node < nodes; node++)
            {
                var nodeOff = nodeOffset + 104*node;


                // 40: input lanes
                // 72: output lanes
                List<int> inputLanes = new List<int>();
                List<int> outputLanes = new List<int>();
                for (int k = 0; k < 8; k++)
                {
                    inputLanes.Add(BitConverter.ToInt32(Stream, 40 + k*4 + nodeOff));
                    outputLanes.Add(BitConverter.ToInt32(Stream, 72 + k*4 + nodeOff));
                }

                // is 8/12 TerrainPointcount and StreamCount??
                // and 0/4 somekind of ID?


                /*var inLanes = string.Join(",", inputLanes.Where(i => i != -1));
                var outLanes = string.Join(",", outputLanes.Where(i => i != -1));

                Console.WriteLine("Node " + node + " X: " + Math.Round(BitConverter.ToSingle(Stream, 16 + nodeOff)) +
                                  " Z: " + Math.Round(BitConverter.ToSingle(Stream, 24 + nodeOff)) + " yaw: " +
                                  Math.Round(
                                      Math.Atan2(BitConverter.ToSingle(Stream, 36 + nodeOff),
                                          BitConverter.ToSingle(Stream, 28 + nodeOff)), 3) + " IN: " + inLanes +
                                  " OUT: " + outLanes);
                */
                Ets2PrefabNode prefabNode = new Ets2PrefabNode
                {
                    Node = node,

                    X = BitConverter.ToSingle(Stream, 16 + nodeOff),
                    Y = BitConverter.ToSingle(Stream, 20 + nodeOff),
                    Z = BitConverter.ToSingle(Stream, 24 + nodeOff),

                    RotationX = BitConverter.ToSingle(Stream, 28 + nodeOff),
                    RotationY = BitConverter.ToSingle(Stream, 32 + nodeOff),
                    RotationZ = BitConverter.ToSingle(Stream, 36 + nodeOff),

                    InputCurve = inputLanes.Where(i => i != -1).Select(x => Curves[x]).ToList(),
                    OutputCurve = outputLanes.Where(i => i != -1).Select(x => Curves[x]).ToList(),

                    Yaw = Math.PI-
                        Math.Atan2(BitConverter.ToSingle(Stream, 36 + nodeOff),
                            BitConverter.ToSingle(Stream, 28 + nodeOff))
                };
                Nodes.Add(prefabNode);

                for (int k = 0; k < 16; k += 4)
                {
                    var i32 = BitConverter.ToInt32(Stream, k + nodeOff);
                    var u64 = BitConverter.ToUInt64(Stream, k + nodeOff);
                    var f32 = BitConverter.ToSingle(Stream, k + nodeOff);
                    //Console.WriteLine(k + " : INT32: " + i32 + " UINT64: " + u64.ToString("X16") + " FLOAT: " + f32);
                }
            }

            var test = GetRouteOptions(0);

            //Console.WriteLine(Path.GetFileNameWithoutExtension(FilePath) + " has  " + nodes + " nodes, " + navCurves +
            //                  " curves, " + mappoints + " map snap points");

        }

        public bool IsFile(string file)
        {
            return Path.GetFileNameWithoutExtension(file) == Path.GetFileNameWithoutExtension(FilePath);
        }

        private IEnumerable<List<Ets2PrefabCurve>> IterateCurves(IEnumerable<Ets2PrefabCurve> list, Ets2PrefabCurve curve, bool forwardDirection)
        {
            var curves = (forwardDirection ? curve.Next : curve.Prev).Select(x => Curves[x]);

            if (curves.Any())
            {
                foreach (var c in curves)
                {
                    var l = new List<Ets2PrefabCurve>(list);
                    if (list.Contains(c))
                        yield return l;
                    else
                    {
                        l.Add(c);

                        var res = IterateCurves(l, c, forwardDirection);
                        foreach (var r in res)
                            yield return r;
                    }
                }

            }
            else
            {
                yield return list.ToList();
            }
        }

        public IEnumerable<Ets2PrefabRoute> GetRouteOptions(int entryNodeId)
        {
            if (entryNodeId >= Nodes.Count() || entryNodeId < 0)
                return new Ets2PrefabRoute[0];
            var entryNode = Nodes[entryNodeId];

            // This entry node has several entry and exit routes (in/out)
            // IN is driving into
            // OUT is coming out of
            var routes = entryNode.InputCurve.Select(x => IterateCurves(new[] { x}, x, true)).SelectMany(x => x).Select(x => new Ets2PrefabRoute(x, entryNode, FindExitNode(x.LastOrDefault()))).ToList();

            return routes;
        }

        public IEnumerable<Ets2PrefabRoute> GetAllRoutes()
        {
            return Nodes.SelectMany(x => GetRouteOptions(x.Node));
        }

        private Ets2PrefabNode FindExitNode(Ets2PrefabCurve c)
        {
            return Nodes.OrderBy(x => Math.Sqrt(Math.Pow(c.EndX - x.X, 2) + Math.Pow(c.EndZ - x.Z, 2))).FirstOrDefault();
        }

        private Ets2PrefabNode FindStartNode(Ets2PrefabCurve c)
        {
            return Nodes.OrderBy(x => Math.Sqrt(Math.Pow(c.StartX - x.X, 2) + Math.Pow(c.StartZ - x.Z, 2))).FirstOrDefault();
        }

        public Ets2PrefabRoute GetRoute(int entryNode, IndicatorSignal signal)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Ets2PrefabRoute> GetRoute(int entryNode, int exitNode)
        {
            var options = GetRouteOptions(entryNode);

            if (options.Any(x => x.Exit.Node == exitNode))
                return options.Where(x => x.Exit.Node == exitNode);
            else
                return new Ets2PrefabRoute[0];
        }

        public IEnumerable<Ets2Point> GeneratePolygonForRoute(Ets2PrefabRoute route, Ets2Node node,  int nodeOr)
        {
            List<Ets2Point> p = new List<Ets2Point>();

            if (route == null || route.Route == null)
            return p;

            /*
            yaw -= this.Nodes[nodeOr].Yaw;
            yaw += Math.PI/2;
            */
            var xOr = node.X;
            var yOr = node.Z;
            var yaw = node.Yaw - this.Nodes[nodeOr].Yaw + Math.PI / 2;

            foreach (var curve in route.Route)
            {
                var srx = curve.StartX - this.Nodes[nodeOr].X;
                var erx = curve.EndX - this.Nodes[nodeOr].X;
                var sry = curve.StartZ - this.Nodes[nodeOr].Z;
                var ery = curve.EndZ - this.Nodes[nodeOr].Z;

                var sr = (float) Math.Sqrt(srx*srx + sry*sry);
                var er = (float) Math.Sqrt(erx*erx + ery*ery);

                var ans = yaw - Math.Atan2(sry, srx);
                var ane = yaw - Math.Atan2(ery, erx);

                var sx = xOr - sr*(float) Math.Sin(ans);
                var ex = xOr - er*(float) Math.Sin(ane);
                var sy = yOr - sr*(float) Math.Cos(ans);
                var ey = yOr - er*(float) Math.Cos(ane);
                // tmp
                var ps = new Ets2Point[2];
                ps[0] = new Ets2Point(sx, node.Y, sy,(float) ans);
                ps[1] = new Ets2Point(ex, node.Y, ey, (float)ane);

                p.AddRange(ps);
            }

            return p;
        }

        public IEnumerable<IEnumerable<PointF>> GeneratePolygonCurves(Ets2Node node, int nodeOr)
        {
            List<IEnumerable<PointF>> ks = new List<IEnumerable<PointF>>();

            var steps = 16;
            
            if (nodeOr >= this.Nodes.Count)
                nodeOr = 0;
            if (Nodes.Any() == false)
                return ks;

            //Console.WriteLine("Rendering prefab " + Path.GetFileNameWithoutExtension(FilePath) + " Or " + nodeOr + " yaw " + Math.Round(180.0f/Math.PI*yaw));

            var xOr = node.X;
            var yOr = node.Z;
            var yaw = node.Yaw - this.Nodes[nodeOr].Yaw + Math.PI/2;

            foreach (var curve in Curves)
            {

                var ps = new PointF[steps];

                var srx = curve.StartX - this.Nodes[nodeOr].X;
                var erx = curve.EndX - this.Nodes[nodeOr].X;
                var sry = curve.StartZ - this.Nodes[nodeOr].Z;
                var ery = curve.EndZ - this.Nodes[nodeOr].Z;

                var sr = (float) Math.Sqrt(srx * srx + sry * sry);
                var er = (float)Math.Sqrt(erx * erx + ery * ery);

                var ans = yaw - Math.Atan2(sry,srx);
                var ane = yaw - Math.Atan2(ery, erx);

                var sx = xOr - sr * (float)Math.Sin(ans);
                var ex = xOr - er * (float)Math.Sin(ane);
                var sy = yOr - sr * (float)Math.Cos(ans);
                var ey = yOr - er * (float)Math.Cos(ane);
                // tmp
                ps = new PointF[2];
                ps[0] = new PointF(sx, sy);
                ps[1] = new PointF(ex, ey);
                ks.Add(ps);
                continue;

                var tangentSX = (float)Math.Cos(ans) * curve.Length;
                var tangentEX = (float)Math.Cos(ane) * curve.Length;
                var tangentSY = (float)Math.Sin(ans) * curve.Length;
                var tangentEY = (float)Math.Sin(ane) * curve.Length;

                for (int k = 0; k < steps; k++)
                {
                    var s = (float)k / (float)steps;
                    var x = (float)Ets2CurveHelper.Hermite(s, sx, ex, tangentSX, tangentEX);
                    var y = (float)Ets2CurveHelper.Hermite(s, sy, ey, tangentSY, tangentEY);
                    ps[k] = new PointF(x, y);
                }

                ks.Add(ps);
            }
            return ks;
        }
    }
}