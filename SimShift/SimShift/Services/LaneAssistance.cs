using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Ets2SdkClient;
using MathNet.Numerics;
using Microsoft.Win32.SafeHandles;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Dialogs;
using SimShift.Entities;
using SimShift.MapTool;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    /// Experimental module using computer vision to steer vehicle within motorway lanes. This module is NOT functional at all.
    /// The module was aimed at ETS2 to maintain focus on the vehicle's mirrors, and adjusting the heading error and lateral error of the white road side lines to desired values.
    /// Some struggles:
    /// - Computer vision can be hard for various road conditions (dark, fog, rain)
    /// - Some ETS2 mods mess up mirror positions and scale.
    /// - FOV
    /// - Measuring heading and lateral error was inaccurate, partly due to computer vision troubles.
    /// - Control of lateral/heading error was instable.
    /// 
    /// This module needs a lot more work to (ever) work properly.
    /// </summary>
    public class LaneAssistance : IControlChainObj
    {
        private float speed = 0.0f;

        public bool Enabled { get { return true; } }
        public bool Active { get; private set; }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        public double SteerAngle { get; private set; }
        public double LockedSteerAngle { get; private set; }

        public bool ButtonActive { get { return DateTime.Now > ButtonCooldownPeriod; } }

        private SoundPlayer beep = new SoundPlayer(@"..\..\\Resources\Alert.wav");

        public DateTime ButtonCooldownPeriod = DateTime.Now;

        #region Implementation of IControlChainObj


        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.Steering:
                    return Active;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch(c)
            {
                case JoyControls.Steering:
                    return SteerAngle;

                default:
                    return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
            bool wasActive = Active;
            //
            if (Main.GetButtonIn(JoyControls.LaneAssistance) && ButtonActive)
            {
                Active = !Active;
                ButtonCooldownPeriod = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
                LockedSteerAngle = Main.GetAxisIn(JoyControls.Steering);
                Debug.WriteLine("Setting lane assistance to: " + Active);
            }

            var currentSteerAngle = Main.GetAxisIn(JoyControls.Steering);

            // User overrides steering
            if (Active && Math.Abs(currentSteerAngle-LockedSteerAngle)>0.05)
            {
                Active = false;
                Debug.WriteLine("[LA] User override steering");
            }

            if (wasActive && !Active)
            {
                beep.Play();
            }
        }

        private int keepAlive = 0;

        public static Ets2Item currentRoad { get; private set; }
        public static Ets2Point hook { get; private set; }
        public static PointF lookPoint { get; private set; }
        public static double yawRoad { get; private set; }

        private List<Ets2NavigationSegment> NearbySegments = new List<Ets2NavigationSegment>(); 

        public void TickTelemetry(IDataMiner data)
        {
            speed = data.Telemetry.Speed;

            if (true)
            {
                var ets2Tel = (Ets2DataMiner) data;
                var x = ets2Tel.MyTelemetry.Physics.CoordinateX;
                var z = ets2Tel.MyTelemetry.Physics.CoordinateZ;
                var yaw = 2*Math.PI*(ets2Tel.MyTelemetry.Physics.RotationX);
                var lah = 1.5f + ets2Tel.MyTelemetry.Physics.SpeedKmh/100.0f*7.5f;
                x += (float)Math.Sin(yaw)*-lah;
                z += (float)Math.Cos(yaw) * -lah;
                var me = new Ets2Point(x, 0, z, (float)yaw);
                lookPoint = new PointF(x, z);
                // Get map
                var map = Main.LoadedMap;
                var route = dlMap.Route;

                if (map == null || route == null)
                {
                    Active = false;
                    return;
                }

                bool firstTry = true;
                Ets2NavigationSegment activeSegment = default(Ets2NavigationSegment);
                var activeSegmentOption = default(Ets2NavigationSegment.Ets2NavigationSegmentOption);
                float dist = float.MaxValue;

            rescanSegment:
                // Find closest segment
                for (int segI = 0; segI < NearbySegments.Count; segI++)
                {
                    var seg = NearbySegments[segI];
                    if (seg == null)
                        continue;
                    if (!seg.Solutions.Any())
                    {
                        continue;
                    }

                    foreach (var sol in seg.Solutions)
                    {
                        if (sol.HiResPoints == null || !sol.HiResPoints.Any())
                            NearbySegments[segI].GenerateHiRes(sol);

                        var dst = sol.HiResPoints.Min(k => k.DistanceTo(me));
                        if (dist > dst)
                        {
                            dist = dst;
                            activeSegment = NearbySegments[segI];
                            activeSegmentOption = sol;
                        }
                    }

                }
                if (!NearbySegments.Any(k => k != null) || dist > 5)
                {
                    FindNewSegments(route, me);
                    if (firstTry)
                    {
                        firstTry = false;
                        goto rescanSegment;
                    }
                    else
                    {
                        //beep.Play();
                        //Active = false;
                        //return;
                    }
                }

                if (activeSegmentOption == null)
                    return;

                var lineDistanceError = 0.0;
                var angleDistancError = 0.0;

                Ets2Point bestPoint = default(Ets2Point);
                Ets2Point bestPointP1 = default(Ets2Point);
                double bestDistance = double.MaxValue;

                for (var k = 0; k < activeSegmentOption.HiResPoints.Count; k++)
                {
                    var distance = me.DistanceTo(activeSegmentOption.HiResPoints[k]);
                    if (bestDistance > Math.Abs(distance))
                    {
                        bestDistance = Math.Abs(distance);
                        if (k + 1 == activeSegmentOption.HiResPoints.Count)
                        {
                            bestPoint = activeSegmentOption.HiResPoints[k - 1];
                            bestPointP1 = activeSegmentOption.HiResPoints[k];
                        }
                        else
                        {
                            bestPoint = activeSegmentOption.HiResPoints[k];
                            var m = k;
                            do
                            {
                                m++;
                                if (m >= activeSegmentOption.HiResPoints.Count)
                                    break;
                                bestPointP1 = activeSegmentOption.HiResPoints[m];
                            } while (bestPoint.DistanceTo(bestPointP1) < 0.1f && m + 1 < activeSegmentOption.HiResPoints.Count);
                        }
                    }
                }
                var min = activeSegmentOption.HiResPoints.Min(k => k.DistanceTo(me));
                if (bestPoint == null)
                    return;

                var lx1 = bestPoint.X - Math.Sin(-bestPoint.Heading) * 5;
                var lz1 = bestPoint.Z - Math.Cos(-bestPoint.Heading) * 5;
                var lx2 = bestPoint.X + Math.Sin(-bestPoint.Heading) * 5;
                var lz2 = bestPoint.Z + Math.Cos(-bestPoint.Heading) * 5;

                lx2 = bestPoint.X;
                lx1 = bestPointP1.X;
                lz2 = bestPoint.Z;
                lz1 = bestPointP1.Z;

                var px1 = me.X - lx1;
                var pz1 = me.Z - lz1;
                var px2 = lz2 - lz1;
                var pz2 = -(lx2 - lx1);
                var qwer = Math.Sqrt(px2*px2 + pz2*pz2);
                Console.WriteLine(qwer);
                // Reference to top (otherwise 90deg offset) - CCW
                yawRoad = activeSegment.Type == Ets2NavigationSegmentType.Road ? -bestPoint.Heading + Math.PI/2 : bestPoint.Heading - Math.PI/2;

                hook = bestPoint;
                lineDistanceError = (px1*px2 + pz1*pz2)/Math.Sqrt(px2*px2 + pz2*pz2);
                angleDistancError = yaw - yawRoad;
                angleDistancError = angleDistancError%(Math.PI*2);
                 //lineDistanceError = -lineDistanceError;
                if (lineDistanceError > 7) lineDistanceError = 7;
                if (lineDistanceError < -7) lineDistanceError = -7;
                //if (Math.Abs(angleDistancError) < Math.PI/4) lineDistanceError = -lineDistanceError;
                Console.WriteLine(lineDistanceError.ToString("0.00m") + " | " + angleDistancError.ToString("0.000rad"));

                var gain = 2.5f + ets2Tel.Telemetry.Speed/2.5f;

                SteerAngle = 0.5f - lineDistanceError/gain;// - angleDistancError * 0.1f;
                //Debug.WriteLine(lineDistanceError + "px error / " + angleDistancError + " angle error / " + SteerAngle);
            }
        }

        private void FindNewSegments(Ets2NavigationRoute route, Ets2Point me)
        {
            if (route == null || route.Segments == null)
                return;
            var segs = new List<Ets2NavigationSegment>();

            var dstLimit = 1250;
            rescan:

            foreach (var seg in route.Segments)
            {
                var dstEntry = seg.Entry.Point.DistanceTo(me) < dstLimit;
                var dstExit = seg.Exit.Point.DistanceTo(me) < dstLimit;

                if (dstEntry || dstExit)
                    segs.Add(seg);
            }
            if (!segs.Any() && dstLimit == 1250)
            {
                dstLimit = 5000;
                goto rescan;
            }
            NearbySegments = segs;
        }

        private float RoadDistance(Ets2Item road, float x, float y)
        {
            if (road == null)
                return float.MaxValue;
            if (road.StartNode == null || road.EndNode == null)
                return float.MaxValue;

            if (Math.Abs(road.StartNode.X - x) > 500)
                return float.MaxValue;
            if (Math.Abs(road.StartNode.Z - y) > 500)
                return float.MaxValue;
            if (road.RoadPolygons == null)
                road.GenerateRoadPolygon(64);

            var minPerPoint = float.MaxValue;

            foreach (var pt in road.RoadPolygons)
            {

                var dx1 = pt.X - x;
                var dy1 = pt.Y - y;
                var r1 = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1);

                if (minPerPoint >= r1)
                    minPerPoint = r1;
            }

            return minPerPoint;
        }

        private bool OutsideRoad(Ets2Item road, float x, float y)
        {
            if (road == null)
                return true;
            if (road.StartNode == null || road.EndNode == null)
                return true;
            var minX = Math.Min(road.StartNode.X, road.EndNode.X);
            var maxX = Math.Max(road.StartNode.X, road.EndNode.X);
            var minY = Math.Min(road.StartNode.Z, road.EndNode.Z);
            var maxY = Math.Max(road.StartNode.Z, road.EndNode.Z);

            var margin = 10.5f;
            if (minX - margin >= x && maxX + margin <= x &&
                minY - margin >= y && maxY + margin <= y)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion

    }
}
