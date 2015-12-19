using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace SimShift.MapTool
{
    public class Ets2NavigationSegment
    {
        public Ets2NavigationSegmentType Type;

        public float Length;
        public float Weight;

        /*** SEGMENT ITEM ***/
        public List<Ets2Item> Roads = new List<Ets2Item>();
        public Ets2Item Prefab;

        /*** NODE ***/
        public Ets2Node Entry;
        public Ets2Node Exit;

        public override string ToString()
        {
            return (Solutions.Count()) + " NAVSEG " + Type.ToString() + " " + ((Type == Ets2NavigationSegmentType.Road)
                ? (Roads.Count() + " roads / " + Entry.NodeUID.ToString("X16") + " > " + Exit.NodeUID.ToString("X16"))
                : Prefab.ItemUID.ToString("X16"));
        }

        /*** SOLUTIONS ***/
        public class Ets2NavigationSegmentOption
        {
            public List<Ets2Point> Points = new List<Ets2Point>();
            public List<Ets2Point> HiResPoints = new List<Ets2Point>();

            public int EntryLane;
            public int ExitLane;
            
            // only valid for road segments
            public bool LaneCrossOver;

            public bool Valid = false;
            public bool LeftLane { get; set; }

            public override string ToString()
            {
                return "NAVSEGOPT " + EntryLane + " > " + ExitLane + " (Valid: " + Valid + ")";
            }
        }

        public List<Ets2NavigationSegmentOption> Options = new List<Ets2NavigationSegmentOption>();
        public IEnumerable<Ets2NavigationSegmentOption> Solutions { get { return Options.Where(x => x.Valid); }} 

        public Ets2NavigationSegment(Ets2Item prefab)
        {
            Type = Ets2NavigationSegmentType.Prefab;

            Prefab = prefab;
        }

        public Ets2NavigationSegment(IEnumerable<Ets2Item> roadPath, Ets2NavigationSegment prevSeg)
        {
            Type = Ets2NavigationSegmentType.Road;

            Roads = roadPath.ToList();

            // Generate entry/exit noads & road item order
            var firstRoad = Roads.FirstOrDefault();
            var lastRoad = Roads.LastOrDefault();

            if (prevSeg.Prefab.NodesList.ContainsValue(firstRoad.StartNode))
            {
                Entry = firstRoad.StartNode;
                Exit = lastRoad.EndNode;
                ReversedRoadChain = false;
                ReversedRoadElements = false;
            }
            else if (prevSeg.Prefab.NodesList.ContainsValue(firstRoad.EndNode))
            {
                Entry = firstRoad.EndNode;
                Exit = lastRoad.StartNode;
                ReversedRoadChain = false;
                ReversedRoadElements = true;
            }
            else if (prevSeg.Prefab.NodesList.ContainsValue(lastRoad.StartNode))
            {
                Entry = lastRoad.StartNode;
                Exit = firstRoad.EndNode;
                ReversedRoadChain = true;
                ReversedRoadElements = false;
            }
            else if ( prevSeg.Prefab.NodesList.ContainsValue(lastRoad.EndNode))
            {
                Entry = lastRoad.EndNode;
                Exit = firstRoad.StartNode;
                ReversedRoadChain = true;
                ReversedRoadElements = true;
            }
            else
            {

            }
        }

        public bool ReversedRoadChain { get; set; }
        public bool ReversedRoadElements { get; set; }

        public bool Match(int mySolution, Ets2NavigationSegment prefabSegment)
        {

            throw new NotImplementedException();
        }

        public bool MatchEntry(int solI, Ets2NavigationSegment prev)
        {
            if (this.Type == Ets2NavigationSegmentType.Road)
            {
                var entryPoint = Options[solI].Points.FirstOrDefault();
                bool res = false;
                foreach (var route in prev.Options)
                {
                    var last = route.Points.LastOrDefault();
                    if (last.CloseTo(entryPoint))
                    {
                        route.EntryLane = this.Options[solI].ExitLane;
                        if (route.ExitLane >= 0 && route.EntryLane >= 0)
                            route.Valid = true;
                        res = true;
                    }
                }

                return res;
            }
            else
            {
                return false;
            }
        }

        public bool MatchExit(int solI, Ets2NavigationSegment next)
        {
            if (this.Type == Ets2NavigationSegmentType.Road)
            {
                var exitPoint = Options[solI].Points.LastOrDefault();
                bool res = false;
                foreach (var route in next.Options)
                {
                    var first = route.Points.FirstOrDefault();
                    if (first.CloseTo(exitPoint))
                    {
                        route.ExitLane = this.Options[solI].EntryLane;
                        if (route.ExitLane >= 0 && route.EntryLane >= 0)
                            route.Valid = true;
                        res = true;
                    }
                }

                return res;
            }
            else
            {
                return false;
            }
        }

        public void GenerateOptions(Ets2NavigationSegment prevSeg, Ets2NavigationSegment nextSeg)
        {
            if (Type == Ets2NavigationSegmentType.Prefab)
            {
                var entryNode = -1;
                var exitNode = -1;
                var i = 0;
                // find node id's
                foreach (var kvp in Prefab.NodesList)
                {
                    if (Entry != null && kvp.Value.NodeUID == Entry.NodeUID) entryNode = i;
                    if (Exit != null && kvp.Value.NodeUID == Exit.NodeUID) exitNode = i;
                    i++;
                }

                //var routes = Prefab.Prefab.GetRoute(entryNode, exitNode);
                var routes = Prefab.Prefab.GetAllRoutes();

                if (routes == null || !routes.Any())
                {
                    return;
                }
                // Create options (we do this by just saving paths)
                foreach (var route in routes)
                {
                    var option = new Ets2NavigationSegmentOption();
                    option.EntryLane = -1;
                    option.ExitLane = -1;
                    option.Points =
                        Prefab.Prefab.GeneratePolygonForRoute(route, Prefab.NodesList.FirstOrDefault().Value,
                            Prefab.Origin).ToList();

                    Options.Add(option);
                }
            }

            var curveSize = 32;

            if (Type == Ets2NavigationSegmentType.Road)
            {
                var firstRoad = Roads.FirstOrDefault();

                // TODO: support UK
                // We have x number of lanes
                for (int startLane = 0; startLane <  firstRoad.RoadLook.LanesRight; startLane++)
                {
                    var curve1 = new List<Ets2Point>();
                    foreach (var rd in Roads)
                    {
                        var rdc = rd.GenerateRoadCurve(curveSize, false, startLane);

                        if (!curve1.Any())
                        {
                            if (!Entry.Point.CloseTo(rdc.FirstOrDefault())) rdc = rdc.Reverse();
                        }
                        else
                        {
                            var lp = curve1.LastOrDefault();
                            if (!rdc.FirstOrDefault().CloseTo(lp))
                                rdc = rdc.Reverse();
                        }
                        
                        curve1.AddRange(rdc);
                    }

                    for (int endLane = 0; endLane <  firstRoad.RoadLook.LanesRight; endLane++)
                    {
                        var curve2 = new List<Ets2Point>();
                        foreach (var rd in Roads)
                        {
                            var rdc = rd.GenerateRoadCurve(curveSize, false, endLane);

                            if (!curve2.Any())
                            {
                                if (!Entry.Point.CloseTo(rdc.FirstOrDefault())) rdc = rdc.Reverse();
                            }
                            else
                            {
                                var lp = curve2.LastOrDefault();
                                if (!rdc.FirstOrDefault().CloseTo(lp))
                                    rdc = rdc.Reverse();
                            }

                            curve2.AddRange(rdc);
                        }

                        var curve = new List<Ets2Point>();
                        curve.AddRange(curve1.Skip(0).Take(curve2.Count/2));
                        curve.AddRange(curve2.Skip(curve2.Count / 2).Take(curve2.Count / 2));
                        if (ReversedRoadChain) curve.Reverse();

                        var option = new Ets2NavigationSegmentOption();
                        option.LeftLane = false;
                        option.EntryLane = startLane;
                        option.ExitLane = endLane;
                        option.Points = curve;
                        option.LaneCrossOver = (startLane != endLane);

                        Options.Add(option);
                    }
                }
                for (int startLane = 0; startLane <  firstRoad.RoadLook.LanesLeft; startLane++)
                {
                    var curve1 = new List<Ets2Point>();
                    foreach (var rd in Roads)
                    {
                        var rdc = rd.GenerateRoadCurve(curveSize, true, startLane);
                        if (!curve1.Any())
                        {
                            if (!Entry.Point.CloseTo(rdc.FirstOrDefault())) rdc = rdc.Reverse();
                        }
                        else
                        {
                            var lp = curve1.LastOrDefault();
                            if (!rdc.FirstOrDefault().CloseTo(lp))
                                rdc = rdc.Reverse();
                        }
                        curve1.AddRange(rdc);
                    }
                    
                    for (int endLane = 0; endLane <  firstRoad.RoadLook.LanesLeft; endLane++)
                    {
                        var curve2 = new List<Ets2Point>();
                        foreach (var rd in Roads)
                        {
                            var rdc = rd.GenerateRoadCurve(curveSize, true, endLane);
                            if (!curve2.Any())
                            {
                                if (!Entry.Point.CloseTo(rdc.FirstOrDefault())) rdc = rdc.Reverse();
                            }
                            else
                            {
                                var lp = curve2.LastOrDefault();
                                if (!rdc.FirstOrDefault().CloseTo(lp))
                                    rdc = rdc.Reverse();
                            }
                            curve2.AddRange(rdc);
                        }

                        var curve = new List<Ets2Point>();
                        curve.AddRange(curve1.Skip(0).Take(curve2.Count / 2));
                        curve.AddRange(curve2.Skip(curve2.Count / 2).Take(curve2.Count / 2));
                        if (!ReversedRoadChain) curve.Reverse();

                        var option = new Ets2NavigationSegmentOption();
                        option.LeftLane = true;
                        option.EntryLane = startLane;
                        option.ExitLane = endLane;
                        option.Points = curve;
                        option.LaneCrossOver = (startLane != endLane);

                        Options.Add(option);
                    }
                }
            }
        }

        public void GenerateHiRes(Ets2NavigationSegmentOption opt)
        {
            var pts = 512;
            if (Type == Ets2NavigationSegmentType.Road)
            {
                var curve1 = new List<Ets2Point>();
                foreach (var rd in Roads)
                {
                    var rdc = rd.GenerateRoadCurve(pts, opt.LeftLane, opt.EntryLane);
                    if (!curve1.Any())
                    {
                        if (!Entry.Point.CloseTo(rdc.FirstOrDefault())) rdc = rdc.Reverse();
                    }
                    else
                    {
                        var lp = curve1.LastOrDefault();
                        if (!rdc.FirstOrDefault().CloseTo(lp))
                            rdc = rdc.Reverse();
                    }
                    curve1.AddRange(rdc);
                }
                var curve2 = new List<Ets2Point>();
                foreach (var rd in Roads)
                {
                    var rdc = rd.GenerateRoadCurve(pts, opt.LeftLane, opt.ExitLane);
                    if (!curve2.Any())
                    {
                        if (!Entry.Point.CloseTo(rdc.FirstOrDefault())) rdc = rdc.Reverse();
                    }
                    else
                    {
                        var lp = curve2.LastOrDefault();
                        if (!rdc.FirstOrDefault().CloseTo(lp))
                            rdc = rdc.Reverse();
                    }
                    curve2.AddRange(rdc);
                }

                var curve = new List<Ets2Point>();
                curve.AddRange(curve1.Skip(0).Take(curve2.Count / 2));
                curve.AddRange(curve2.Skip(curve2.Count / 2).Take(curve2.Count / 2));
                if (ReversedRoadChain) curve.Reverse();

                opt.HiResPoints = curve;
            }
            if (Type == Ets2NavigationSegmentType.Prefab)
            {
                opt.HiResPoints = opt.Points;
            }
        }
    }
}