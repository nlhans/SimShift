using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SimShift.MapTool
{
    public class Ets2NavigationRoute
    {
        public Ets2Item Start;
        public Ets2Item End;

        public Ets2Mapper Mapper;
        public bool Loading = false;

        public List<Tuple<Ets2Item, int, int>> Prefabs;
        public List<Ets2Item> Roads;

        public List<Ets2NavigationSegment> Segments;

        private Ets2Point From;
        private Ets2Point To;
 

        public Ets2NavigationRoute(Ets2Item start, Ets2Item end, Ets2Point from, Ets2Point to, Ets2Mapper mapper)
        {
            Start = start;
            End = end;
            From = from;
            To = to;
            Mapper = mapper;

            if (Start != End)
            ThreadPool.QueueUserWorkItem(new WaitCallback(FindRoute));
        }

        private void FindRoute(object state)
        {
            Loading = true;

            Dictionary<ulong, Tuple<float, Ets2Item>> nodeMap = new Dictionary<ulong, Tuple<float, Ets2Item>>();
            List<Ets2Item> nodesToWalk = new List<Ets2Item>();

            // Fill node map
            foreach (var node in Mapper.Items.Values.Where(x => x.Type == Ets2ItemType.Prefab && x.HideUI == false))
            {
                nodesToWalk.Add(node);
                nodeMap.Add(node.ItemUID, new Tuple<float, Ets2Item>(float.MaxValue, null));
            }

            // Walk first node (START)
            if (nodeMap.ContainsKey(Start.ItemUID) == false)
            {
                // Nope
                return;
            }
            if (nodeMap.ContainsKey(End.ItemUID) == false)
            {
                // Nope
                return;
            }

            // <Weight, LastItem>
            nodeMap[Start.ItemUID] = new Tuple<float, Ets2Item>(0, null);

            while (nodesToWalk.Any())
            {
                float distanceWalked = float.MaxValue;
                Ets2Item toWalk = null;
                foreach (var node in nodesToWalk)
                {
                    var dTmp = nodeMap[node.ItemUID].Item1;
                    if (dTmp != float.MaxValue && distanceWalked > dTmp)
                    {
                        distanceWalked = dTmp;
                        toWalk = node;
                    }
                }
                if (toWalk == null)
                    break;

                nodesToWalk.Remove(toWalk);

                var currentWeight = nodeMap[toWalk.ItemUID].Item1;

                // Iterate all destination nodes from this node
                foreach (var jump in toWalk.Navigation)
                {
                    var newWeight = jump.Value.Item1 + currentWeight;
                    var newNode = jump.Key;

                    if (nodeMap.ContainsKey(newNode.ItemUID) && nodeMap[newNode.ItemUID].Item1 > newWeight)
                        nodeMap[newNode.ItemUID] = new Tuple<float, Ets2Item>(newWeight, toWalk);
                            // add route with weight + previous node
                }
            }

            List<Ets2Item> routeNodes = new List<Ets2Item>();
            List<Ets2Item> routeRoads = new List<Ets2Item>();
            List<Ets2NavigationSegment> segments = new List<Ets2NavigationSegment>();

            var goingViaNode = (ulong) 0;
            var route = End;

            while (route != null)
            {
                // we add this prefab to the route list
                routeNodes.Add(route);
                var prefabSeg = new Ets2NavigationSegment(route);
                segments.Add(prefabSeg);
                
                // find the next prefab in the route description
                var gotoNew = nodeMap[route.ItemUID].Item2;
                if (gotoNew == null) break;

                // get a path from the current prefab to the new one
                var path = route.Navigation[gotoNew];
                var roadPath = path.Item3;

                // add the path to road list
                routeRoads.AddRange(roadPath);
                segments.Add(new Ets2NavigationSegment(roadPath, prefabSeg));

                // Set the new prefab as route
                route = gotoNew;
            }
            routeNodes.Add(Start);
            segments.Reverse();

            // Find entry/exit of start/end segment
            var foundDst = float.MaxValue;
            var foundNode = default(Ets2Node);
            // Find the closest road to startpoint
            foreach (var node in segments[0].Prefab.NodesList)
            {
                var dst = node.Value.Point.DistanceTo(From);
                if (foundDst > dst)
                {
                    foundDst = dst;
                    foundNode = node.Value;
                }
            }

            // We found the  node; find the road that exists at this point
            segments[0].Entry = foundNode;

            foundDst = float.MaxValue;
            foundNode = default(Ets2Node);
            // Find the closest road to startpoint
            foreach (var node in segments[segments.Count - 1].Prefab.NodesList)
            {
                var dst = node.Value.Point.DistanceTo(To);
                if (foundDst > dst)
                {
                    foundDst = dst;
                    foundNode = node.Value;
                }
            }

            // We found the  node; find the road that exists at this point
            segments[segments.Count - 1].Exit = foundNode;

            // Iterate all segments
            for (int seg = 0; seg < segments.Count; seg++)
            {
                // Generate prefab routes
                if (segments[seg].Type == Ets2NavigationSegmentType.Prefab)
                {
                    var prevRoad = seg > 0 ? segments[seg - 1] : null;
                    var nextRoad = seg + 1 < segments.Count ? segments[seg + 1] : null;


                    // Link segments together
                    if (prevRoad != null) segments[seg].Entry = prevRoad.Entry;
                    if (nextRoad != null) segments[seg].Exit = nextRoad.Exit;

                    segments[seg].GenerateOptions(prevRoad,nextRoad);
                }

                // Generate lane data
                if (segments[seg].Type == Ets2NavigationSegmentType.Road)
                {
                    var prefFab = seg > 0 ? segments[seg - 1] : null;
                    var nextFab = seg + 1 < segments.Count ? segments[seg + 1] : null;

                segments[seg].GenerateOptions(prefFab, nextFab);
                }
            }

            //for (int seg = 1; seg < segments.Count - 1; seg++)
            for (int seg = 0; seg < segments.Count; seg++)
            {
                // Validate routes
                if (segments[seg].Type == Ets2NavigationSegmentType.Prefab)
                {
                    var prevRoad = seg > 0 ? segments[seg - 1] : null;
                    var nextRoad = seg + 1 < segments.Count ? segments[seg + 1] : null;

                    foreach (var opt in segments[seg].Options)
                    {
                        if (prevRoad == null)
                        {
                            // start point; validate if it is close to our start node
                            if (opt.Points.FirstOrDefault().DistanceTo(segments[seg].Entry.Point) < 10)
                            {
                                opt.EntryLane = 0; // yep; sure
                            }
                        }
                        else
                        {
                            var entryPoint = opt.Points.FirstOrDefault();
                            foreach (var roadOpt in prevRoad.Options)
                            {
                                if (roadOpt.Points.Any(x => x.CloseTo(entryPoint)))
                                {
                                    // We've got a match ! :D
                                    opt.EntryLane = roadOpt.ExitLane;
                                    roadOpt.Valid = roadOpt.EntryLane >= 0 && roadOpt.ExitLane >= 0;
                                }
                            }
                        }

                        if (nextRoad == null)
                        {
                            // last point.
                            if (opt.Points.FirstOrDefault().DistanceTo(segments[seg].Exit.Point) < 10)
                            {
                                opt.ExitLane = 0; // yep; sure
                            }
                        }
                        else
                        {
                            var exitPoint = opt.Points.LastOrDefault();
                            foreach (var roadOpt in nextRoad.Options)
                            {
                                if (roadOpt.Points.Any(x => x.CloseTo(exitPoint)))
                                {
                                    // We've got a match ! :D
                                    opt.ExitLane= roadOpt.EntryLane;
                                    roadOpt.Valid = roadOpt.EntryLane >= 0 && roadOpt.ExitLane >= 0;
                                }
                            }
                        }

                        opt.Valid = opt.EntryLane >= 0 && opt.ExitLane >= 0;
                    }
                    

                }

                // Generate prefab routes
                if (segments[seg].Type == Ets2NavigationSegmentType.Road && false)
                {
                    var nextPrefab = segments[seg + 1];
                    var prevPrefab = segments[seg - 1];

                    if (nextPrefab.Type != Ets2NavigationSegmentType.Prefab ||
                        prevPrefab.Type != Ets2NavigationSegmentType.Prefab)
                        continue;

                    // Deduct road options by matching entry/exits
                    for (int solI = 0; solI < segments[seg].Options.Count; solI++)
                    {
                        // Find if there is any route in prefab that matches our entry lane
                        var okStart = prevPrefab.Options.Any(x => segments[seg].MatchEntry(solI, prevPrefab));
                        var okEnd = nextPrefab.Options.Any(x => segments[seg].MatchExit(solI, nextPrefab));

                        // This road has a valid end & Start
                        if (okStart && okEnd)
                        {
                            segments[seg].Options[solI].Valid = true;
                        }
                    }
                }
            }

            // There is probably only 1 valid solution for entry/exit
            //if (segments[0].Options.Any()) segments[0].Options[0].Valid = true;
            //if (segments[segments.Count - 1].Options.Any()) segments[segments.Count - 1].Options[0].Valid = true;

            Segments = segments;
            var pts = Segments.SelectMany(x => x.Solutions).SelectMany(x => x.Points).ToList();
            Roads = routeRoads;
            Prefabs = routeNodes.Select(x => new Tuple<Ets2Item, int, int>(x, 0, 0)).ToList();
            Loading = false;
        }

    }
}