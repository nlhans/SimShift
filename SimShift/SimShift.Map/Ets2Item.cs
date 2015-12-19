using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SimShift.MapTool
{
    public class Ets2Item
    {
        public Ets2Sector Sector { get; private set; }
        public ulong ItemUID { get; private set; }
        public Ets2ItemType Type { get; set; }

        public string FilePath { get; set; }
        public int FileOffset { get; set; }
        public int BlockSize { get; private set; }
        public bool Valid { get; private set; }

        public bool HideUI { get; private set; }

        public Dictionary<ulong, Ets2Node> NodesList { get; private set; }

        // Dictionary <Prefab> , <NavigationWeight, Length, RoadList>>
        public Dictionary<Ets2Item, Tuple<float, float, IEnumerable<Ets2Item>>> Navigation { get; private set; } 

        public Ets2Node StartNode { get; private set; }
        public ulong StartNodeUID { get; private set; }
        public Ets2Node EndNode { get; private set; }
        public ulong EndNodeUID { get; private set; }
        public Ets2Node PrefabNode { get; set; }
        public ulong PrefabNodeUID { get; set; }

        /** Item specific values/interpretations **/

        // Prefab type
        public Ets2Prefab Prefab { get; private set; }
        public int Origin = 0;

        // Road info
        public Ets2RoadLook RoadLook { get; private set; }
        public IEnumerable<PointF> RoadPolygons { get; private set; }

        // City/company info
        public string City { get; private set; }
        public string Company { get; set; }

        public Ets2Item(ulong uid, Ets2Sector sector, int offset)
        {
            ItemUID = uid;

            Navigation = new Dictionary<Ets2Item, Tuple<float, float, IEnumerable<Ets2Item>>>();

            Sector = sector;
            FileOffset = offset;
            FilePath = sector.FilePath;

            NodesList = new Dictionary<ulong, Ets2Node>();
                    
            Type = (Ets2ItemType)BitConverter.ToUInt32(sector.Stream, offset);

            int nodeCount;

            switch (Type)
            {
                case Ets2ItemType.Road:
                    StartNodeUID = BitConverter.ToUInt64(sector.Stream, offset + 141);
                    EndNodeUID = BitConverter.ToUInt64(sector.Stream, offset + 149);

                    var lookId = BitConverter.ToUInt32(sector.Stream, offset + 61); // unique UINT32 ID with road look
                    RoadLook = Sector.Mapper.LookupRoadLookID(lookId);

                    // Need to create LUT to translate road_look.sii <> ID
                    // Then we can parse highway routes etc.

                    HideUI = (sector.Stream[offset + 0x37] & 0x02) != 0;

                    // Make sure these UID's exist in the world.
                    if ((StartNodeUID != 0 && sector.Mapper.Nodes.ContainsKey(StartNodeUID)) ||
                        (EndNodeUID != 0 && sector.Mapper.Nodes.ContainsKey(EndNodeUID)))
                    {
                        Valid = true;

                        var stamps = BitConverter.ToInt32(sector.Stream, offset + 433);
                        BlockSize = 437 + stamps*24;
                    }
                    else
                    {
                        Valid = false;
                    }
                    break;

                case Ets2ItemType.Prefab:
                    if (uid == 0x2935de9c700704)
                    {
                        //
                    }

                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 81);
                    HideUI = (sector.Stream[offset + 0x36] & 0x02) != 0;

                    if (nodeCount > 0x20)
                    {
                        Valid = false;
                        return;
                    }
                    var somethingOffset = offset + 85 + 8*nodeCount;
                    if (somethingOffset < offset || somethingOffset > sector.Stream.Length)
                    {
                        Valid = false;
                        return;
                    }
                    var something = BitConverter.ToInt32(sector.Stream,somethingOffset);

                    if (something < 0 || something > 32)
                    {
                        Valid = false;
                        return;
                    }
                    var OriginOffset = offset + 0x61 + nodeCount*8 + something*8;
                    if (OriginOffset < offset || OriginOffset > sector.Stream.Length)
                    {
                        Valid = false;
                        return;
                    }
                    Origin = sector.Stream[OriginOffset] & 0x03;
                    //Console.WriteLine("PREFAB @ " + uid.ToString("X16") + " origin: " + Origin);
                    var prefabId = (int)BitConverter.ToUInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20 && nodeCount != 0)
                    {
                        Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 81 + 4 + i*8);
                            //Console.WriteLine("prefab node link " + i + ": " + nodeUid.ToString("X16"));
                            // TODO: if node is in other sector..
                            if (AddNodeUID(nodeUid) == false)
                            {
                                //Console.WriteLine("Could not add prefab node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }
                        PrefabNodeUID = NodesList.Keys.FirstOrDefault();
                    }


                    //Console.WriteLine("PREFAB ID: " + prefabId.ToString("X8"));
                    Prefab = sector.Mapper.LookupPrefab(prefabId);
                    if (Prefab == null)
                    {
                        //Console.WriteLine("Prefab ID: " + uid.ToString("X16") + " / " + prefabId.ToString("X") +
                        //                  " not found");
                    }
                    break;

                case Ets2ItemType.Company:
                    Valid = true;

                    // There are 3 nodes subtracted from found in sector:
                    // 1) The node of company itself
                    // 2) The node of loading area
                    // 3) The node of job 
                    nodeCount = Sector.Nodes.Count(x => x.ForwardItemUID == uid) - 2;
                    BlockSize = nodeCount*8 + 109;

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        var prefabItemUid = BitConverter.ToUInt64(sector.Stream, offset + 73);
                        var loadAreaNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 93);
                        var jobAreaNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 81);

                        if (AddNodeUID(loadAreaNodeUid) == false)
                        {
                            //Console.WriteLine("Could not add loading area node " + loadAreaNodeUid.ToString("X16"));
                        }
                        else if (AddNodeUID(jobAreaNodeUid) == false)
                        {
                            //Console.WriteLine("Could not add job area node" + jobAreaNodeUid.ToString("X16"));
                        }
                        else
                        {
                            for (int i = 0; i < nodeCount; i++)
                            {
                                var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 113 + i*8);
                                //Console.WriteLine("company node link " + i + ": " + nodeUid.ToString("X16"));
                                if (AddNodeUID(nodeUid) == false)
                                {
                                    //Console.WriteLine("Could not add cargo area node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Valid = false;
                    }

                    break;


                case Ets2ItemType.Building:
                    var buildingNodeUid1 = BitConverter.ToUInt64(sector.Stream, offset + 73);
                    var buildingNodeUid2 = BitConverter.ToUInt64(sector.Stream, offset + 65);
                    Valid = AddNodeUID(buildingNodeUid1) && AddNodeUID(buildingNodeUid2);
                    BlockSize = 97;
                    break;

                case Ets2ItemType.Sign:
                    var signNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 65);
                    BlockSize = 153;
                    Valid = AddNodeUID(signNodeUid);
                    break;

                case Ets2ItemType.Model:
                    var modelNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 81);
                    BlockSize = 101;
                    Valid = AddNodeUID(modelNodeUid);
                    break;

                case Ets2ItemType.MapOverlay:
                    var mapOverlayNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 65);
                    BlockSize = 73;
                    Valid = AddNodeUID(mapOverlayNodeUid);
                    break;

                case Ets2ItemType.Ferry:
                    var ferryNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 73);
                    BlockSize = 93;
                    Valid = AddNodeUID(ferryNodeUid);
                    break;

                case Ets2ItemType.CutPlane:

                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57 + 4 + i * 8);
                            //Console.WriteLine("cut plane node " + i + ": " + nodeUid.ToString("X16"));
                            if (AddNodeUID(nodeUid) == false)
                            {
                                //Console.WriteLine("Could not add cut plane node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }
                    }

                    BlockSize = 61 + 8 * nodeCount;
                    break;

                case Ets2ItemType.TrafficRule:
                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57 + 4 + i * 8);
                            //Console.WriteLine("traffic area node " + i + ": " + nodeUid.ToString("X16"));
                            if (AddNodeUID(nodeUid) == false)
                            {
                                //Console.WriteLine("Could not add traffic area node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }
                    }

                    BlockSize = 73 + 8 * nodeCount;
                    break;

                case Ets2ItemType.Trigger:
                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57 + 4 + i * 8);
                            //Console.WriteLine("trigger node " + i + ": " + nodeUid.ToString("X16"));
                            if (AddNodeUID(nodeUid) == false)
                            {
                                //Console.WriteLine("Could not add trigger node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }
                    }

                    BlockSize = 117 + 8 * nodeCount;
                    break;

                case Ets2ItemType.BusStop:
                    var busStopUid = BitConverter.ToUInt64(sector.Stream, offset + 73);
                    BlockSize = 81;
                    Valid = AddNodeUID(busStopUid);
                    break;

                case Ets2ItemType.Garage:
                    // TODO: at offset 65 there is a int '1' value.. is it a list?
                    var garageUid = BitConverter.ToUInt64(sector.Stream, offset + 69);
                    BlockSize = 85;
                    Valid = AddNodeUID(garageUid);
                    break;

                case Ets2ItemType.FuelPump:
                    var dunno2Uid = BitConverter.ToUInt64(sector.Stream, offset + 57);
                    BlockSize = 73;
                    Valid = AddNodeUID(dunno2Uid);
                    break;

                case Ets2ItemType.Dunno:
                    Valid = true;
                    break;

                case Ets2ItemType.Service:
                    var locationNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57);
                    Valid = AddNodeUID(locationNodeUid);
                    BlockSize = 73;
                    break;

                    case Ets2ItemType.City:
                    var CityID = BitConverter.ToUInt64(sector.Stream, offset + 57);
                    var NodeID = BitConverter.ToUInt64(sector.Stream, offset + 73);
                    
                    if ((CityID >> 56) != 0)
                    {
                        break;
                    }
                    City = Sector.Mapper.LookupCityID(CityID);
                    Valid = City != string.Empty && NodeID != 0 && sector.Mapper.Nodes.ContainsKey(NodeID);
                    if (!Valid)
                    {
                        Console.WriteLine("Unknown city ID " + CityID.ToString("X16") + " at " + ItemUID.ToString("X16"));
                    }
                    else
                    {
                        StartNodeUID = NodeID;
                        //Console.WriteLine(CityID.ToString("X16") + " === " + City);
                    }
                    BlockSize = 81;
                    break;

                default:
                    Valid = false;
                    break;
            }

            //if (Valid)
            //    Console.WriteLine("Item " + uid.ToString("X16") + " (" + Type.ToString() + ") is found at " + offset.ToString("X"));
        }

        /// <summary>
        /// Generate road curves for a specific lane. The curve is generated with [steps] 
        /// nodes and positioned left or right from the road's middle point.
        /// Additionally, each extra lane is shifted 4.5 game units outward.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="leftlane"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        public IEnumerable<Ets2Point> GenerateRoadCurve(int steps, bool leftlane, int lane)
        {
            var ps = new Ets2Point[steps];

            var sx = StartNode.X;
            var ex = EndNode.X;
            var sz = StartNode.Z;
            var ez = EndNode.Z;

            if (steps == 2)
            {
                sx += (float)Math.Sin(-StartNode.Yaw) * (leftlane ? -1 : 1) * (RoadLook.Offset + (0.5f + lane) * 4.5f);
                sz += (float)Math.Cos(-StartNode.Yaw) * (leftlane ? -1 : 1) * (RoadLook.Offset + (0.5f + lane) * 4.5f);

                ex += (float)Math.Sin(-EndNode.Yaw) * (leftlane ? -1 : 1) * (RoadLook.Offset + (0.5f + lane) * 4.5f);
                ez += (float)Math.Cos(-EndNode.Yaw) * (leftlane ? -1 : 1) * (RoadLook.Offset + (0.5f + lane) * 4.5f);

                ps[0] = new Ets2Point(sx, 0, sz, StartNode.Yaw);
                ps[1] = new Ets2Point(ex, 0, ez, EndNode.Yaw);
                return ps;
            }


            var radius = (float)Math.Sqrt((sx - ex) * (sx - ex) + (sz - ez) * (sz - ez));

            var tangentSX = (float)Math.Cos(-StartNode.Yaw) * radius;
            var tangentEX = (float)Math.Cos(-EndNode.Yaw) * radius;
            var tangentSZ = (float)Math.Sin(-StartNode.Yaw) * radius;
            var tangentEZ = (float)Math.Sin(-EndNode.Yaw) * radius;

            for (int k = 0; k < steps; k++)
            {
                var s = (float) k/(float) (steps - 1);
                var x = (float) Ets2CurveHelper.Hermite(s, sx, ex, tangentSX, tangentEX);
                var z = (float) Ets2CurveHelper.Hermite(s, sz, ez, tangentSZ, tangentEZ);
                var tx = (float) Ets2CurveHelper.HermiteTangent(s, sx, ex, tangentSX, tangentEX);
                var ty = (float) Ets2CurveHelper.HermiteTangent(s, sz, ez, tangentSZ, tangentEZ);
                var yaw = (float) Math.Atan2(ty, tx);
                x += (float) Math.Sin(-yaw)*(leftlane ? -1 : 1)*(RoadLook.Offset + (0.5f + lane)*4.5f);
                z += (float) Math.Cos(-yaw)*(leftlane ? -1 : 1)*(RoadLook.Offset + (0.5f + lane)*4.5f);
                ps[k] = new Ets2Point(x, 0, z, yaw);
            }

            return ps;
        }

        public void GenerateRoadPolygon(int steps)
        {
            if (RoadPolygons == null)
                RoadPolygons = new PointF[0];

            if (RoadPolygons != null && RoadPolygons.Count() == steps)
                return;

            if (StartNode == null || EndNode == null)
                return;

            if (Type != Ets2ItemType.Road)
                return;

            var ps = new PointF[steps];

            var sx = StartNode.X;
            var ex = EndNode.X;
            var sy = StartNode.Z;
            var ey = EndNode.Z;

            var radius = (float)Math.Sqrt((sx - ex) * (sx - ex) + (sy - ey) * (sy - ey));

            var tangentSX = (float)Math.Cos(-StartNode.Yaw) * radius;
            var tangentEX = (float)Math.Cos(-EndNode.Yaw) * radius;
            var tangentSY = (float)Math.Sin(-StartNode.Yaw) * radius;
            var tangentEY = (float)Math.Sin(-EndNode.Yaw) * radius;

            for (int k = 0; k < steps; k++)
            {
                var s = (float)k / (float)(steps - 1);
                var x= (float)Ets2CurveHelper.Hermite(s, sx, ex, tangentSX, tangentEX);
                var y = (float)Ets2CurveHelper.Hermite(s, sy, ey, tangentSY, tangentEY);
                ps[k] = new PointF(x, y);
            }

            RoadPolygons = ps;

        }

        private bool AddNodeUID(ulong nodeUid)
        {
            if (nodeUid == 0 || Sector.Mapper.Nodes.ContainsKey(nodeUid)== false)
            {
                Valid = false;
                return false;
            }
            else
            {
                NodesList.Add(nodeUid, null);
                return true;
            }
        }

        public bool Apply(Ets2Node node)
        {
            if (node.NodeUID == PrefabNodeUID)
            {
                PrefabNode = node;
            }

            if (node.NodeUID == StartNodeUID)
            {
                StartNode = node;
                return true;
            }
            else if (node.NodeUID == EndNodeUID)
            {
                EndNode = node;
                return true;
            }
            else if (NodesList.ContainsKey(node.NodeUID))
            {
                NodesList[node.NodeUID] = node;
                return true;
            }
            else
            {
                //Console.WriteLine("Could not apply node " + node.NodeUID.ToString("X16") + " to item " + ItemUID.ToString("X16"));
                return false;
            }
        }

        public override string ToString()
        {
            return "Item #" + ItemUID.ToString("X16") + " (" + Type.ToString() + ")";
        }
    }
}