using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ets2SdkClient;
using MathNet.Numerics.Interpolation;
using SimShift.Data;
using SimShift.MapTool;
using SimShift.Services;
using SimShift.Utils;

namespace SimShift.Dialogs
{
    public partial class dlMap : Form
    {
        public static Ets2NavigationRoute Route{get { return route; }}
        private static Ets2NavigationRoute route;
        private static PointF location;
        private static float mapScale;
        private bool drag;
 private static bool locationOverride;
        private static bool ProcessDoubleClick;
        private static Point DoubleClickPoint;


        private static PointF virtualPoint = new PointF(23741.5f, -1952.64f);
        //private static PointF virtualPoint = new PointF(300,-300);

        public dlMap()
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            Main.Data.DataReceived += Data_DataReceived;
            var updatetimer = new Timer {Interval = 300};
            updatetimer.Tick += updatetimer_Tick;
            updatetimer.Start();

            locationOverride = false;
            location = virtualPoint;
            mapScale = 100.0f;

            MouseDown += dlMap_MouseDown;
            MouseUp += dlMap_MouseUp;
            MouseLeave += dlMap_MouseLeave;
            MouseMove += dlMap_MouseMove;
            MouseWheel += dlMap_MouseWheel;
            MouseDoubleClick += dlMap_MouseDoubleClick;
        }

        void dlMap_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ProcessDoubleClick = true;
            DoubleClickPoint = e.Location;
        }

        private static Point mouseMovePoint;
        private Point mouseDownPoint;

        private void dlMap_MouseMove(object sender, MouseEventArgs e)
        {
            mouseMovePoint = e.Location;
            if (drag)
            {
                locationOverride = true;
                var spd = mapScale/Math.Max(this.Width, this.Height);
                location = new PointF(location.X - (e.X - mouseDownPoint.X)*spd,
                    location.Y - (e.Y - mouseDownPoint.Y)*spd);
                mouseDownPoint = e.Location;

                this.Invalidate();
            }
        }

        private void dlMap_MouseLeave(object sender, EventArgs e)
        {
            drag = false;
        }

        void dlMap_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        void dlMap_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDownPoint = e.Location;
            drag = true;
        }

        void dlMap_MouseWheel(object sender, MouseEventArgs e)
        {
            locationOverride = true;

            mapScale -= e.Delta;
            mapScale = Math.Max(100, Math.Min(30000, mapScale));

            Invalidate();
        }

        private void Data_DataReceived(object sender, EventArgs e)
        {
            if (locationOverride == false)
            {
                var pt = GetLivePoint();
                location = pt.Item1;
                mapScale = pt.Item2;
            }
        }

        private static Tuple<PointF, float> GetLivePoint()
        {
            var ets2Tel = (Main.Data.Active == null)
                ? default(Ets2Telemetry)
                : ((Ets2DataMiner) Main.Data.Active).MyTelemetry;
            if (ets2Tel == null)
            {
                return new Tuple<PointF, float>(virtualPoint, 500);
            }
            else
            {
                var spd = Math.Abs(ets2Tel.Drivetrain.SpeedKmh);
                var scale = 200 + spd*spd*0.15f;
                if (scale > 7500)
                    scale = 7500;
                var loc = new PointF(ets2Tel.Physics.CoordinateX, ets2Tel.Physics.CoordinateZ);
                return new Tuple<PointF, float>(loc, scale);
            }
        }

        private void updatetimer_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;

            var rect = e.ClipRectangle;
            RenderMap(rect, g,true);

            /*var f = new Font("Tahoma", 10.0f);
            g.DrawString(
                string.Format("X{0:00000.00} Z{1:00000.00} {2:000.0}km/h {3}/{6} nodes {4}/{7} items {5} roads", tx, ty,
                    spd, nodesNearby.Count(), itemsNearby.Count(), roads.Count(), map.Nodes.Count, map.Items.Count), f,
                Brushes.White, 10, 10);*/
        }

        private static ulong markedPrefab;

        public static int RenderMap(Rectangle clip, Graphics g, bool dedicated)
        {
            float sc = 5000;
            return RenderMap(clip, g, dedicated, ref sc);
        }

        public static int RenderMap(Rectangle clip, Graphics g, bool dedicated, ref float scale)
        {
            var ets2Tel = (Main.Data.Active == null)
                ? default(Ets2Telemetry)
                : ((Ets2DataMiner)Main.Data.Active).MyTelemetry;

            // Search the map
            var map = FrmMain.Ets2Map;

            g.FillRectangle(map.Loading ? Brushes.DarkOrange : Brushes.Black, clip);
            g.SmoothingMode = mapScale < 1000 ? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.None;

            float tx = 0.0f;
            float ty = 0.0f;
            float baseScale = 0.0f;

            if (Main.Data.Active == null || (dedicated && locationOverride))
            {
                tx = location.X;
                ty = location.Y;
                baseScale = mapScale;
            }
            else
            {
                var d = GetLivePoint();
                tx = d.Item1.X;
                ty = d.Item1.Y;
                baseScale = d.Item2;
            }

            var totalX = 0.0f;
            var totalY = 0.0f;

            // Input scale value is max scale
            var maxScale = scale;
            if (baseScale > maxScale)
                baseScale = scale;
            scale = baseScale;


            if (clip.Width > clip.Height)
            {
                totalX = baseScale;
                totalY = (int)(baseScale * (float)clip.Height / clip.Width);
            }
            else
            {
                totalY = baseScale;
                totalX = (int)(baseScale * (float)clip.Width / clip.Height);
            }

            var startX = clip.X + tx - totalX;
            var endX = clip.X + tx + totalX;
            var startY = clip.Y + ty - totalY;
            var endY = clip.Y + ty + totalY;

            var scaleX = clip.Width / (endX - startX);
            var scaleY = clip.Height / (endY - startY);

            if (float.IsInfinity(scaleX) || float.IsNaN(scaleX))
                scaleX = clip.Width;
            if (float.IsInfinity(scaleY) || float.IsNaN(scaleY))
                scaleY = clip.Height;

            var nodesNearby =
                map.Nodes.Values.Where(
                    x => x.X >= startX - 1500 && x.X <= endX + 1500 && x.Z >= startY -1500 && x.Z <= endY + 1500);
            var itemsNearby = nodesNearby.SelectMany(x => x.GetItems()).Where(x => x.HideUI == false).ToList();

            var roads = itemsNearby.Where(x => x.Type == Ets2ItemType.Road);
            var prefabs = itemsNearby.Where(x => x.Type == Ets2ItemType.Prefab);

            var gpsPen = new Pen(Brushes.MediumPurple, 22*scaleX);
            var localPen = new Pen(Brushes.Orange, 7.5f * scaleX);
            var prefabLane = new Pen(Brushes.Yellow, 3 * scaleX);
            var expressPen = new Pen(Brushes.Yellow, 19 * scaleX);
            var highwayPen = new Pen(Brushes.Red, 22*scaleX);

            List<ulong> nodesPassed = new List<ulong>();
            List<List<PointF>> roadPoints = new List<List<PointF>>();
            var nodesToFollow = prefabs.SelectMany(x => x.NodesList.Values).Distinct();

            // Gather all prefabs, and issue a drawing command
            foreach (var node in nodesToFollow)
            {
                if (node == null)
                    continue;

                bool isHighway = false;
                bool isExpress = false;
                bool isLocal = false;

                // Nodes from prefab are always like:
                // Prefab = Forward
                // Road=backward
                var road = node.ForwardItem != null && node.ForwardItem.Type == Ets2ItemType.Prefab
                    ? node.BackwardItem
                    : node.ForwardItem;
                var roadStart = road;
                isHighway = road == null || road.RoadLook == null ? false : road.RoadLook.IsHighway;
                isExpress = road == null || road.RoadLook == null ? false : road.RoadLook.IsExpress;
                isLocal = road == null || road.RoadLook == null ? false : road.RoadLook.IsLocal;
                var fw = node.ForwardItem != null && node.ForwardItem.Type == Ets2ItemType.Road;

                if (road == null)
                {
                    // DEAD END
                    //Console.WriteLine("Dead-end from prefab..");
                    continue;
                }

                var roadChain = new List<Ets2Item>();

                // Start drawing at start road
                if (fw)
                {
                    do
                    {
                        roadChain.Add(road);
                        road = road.EndNode == null ? null : road.EndNode.ForwardItem;
                    } while (road != null && road.Type == Ets2ItemType.Road);
                }
                else
                {
                    do
                    {
                        roadChain.Add(road);
                        road = road.StartNode == null ? null : road.StartNode.BackwardItem;
                    } while (road != null && road.Type == Ets2ItemType.Road);
                }

                if (!fw)
                    roadChain.Reverse();

                foreach (var n in roadChain.Where(x => x.HideUI == false))
                {
                    n.GenerateRoadPolygon(64);
                }
                var pen = isHighway ? highwayPen : isExpress ? expressPen : localPen;

                var roadPoly =
                    roadChain.Where(x => x.HideUI == false)
                        .SelectMany(x => x.RoadPolygons)
                        .Select(x => new PointF((x.X - startX)*scaleX, (x.Y - startY)*scaleY));

                if (roadPoly.Any())
                {
                    g.DrawLines(pen, roadPoly.ToArray());
                }
            }

            // Draw GPS routes if any
            if (route != null && route.Segments != null && route.Segments.Any())
            {
                foreach (var seg in route.Segments)
                {
                    if (seg.Solutions.Any())
                    {
                        foreach (var opt in seg.Solutions)
                        {
                            var pt = opt.Points.Select(x => new PointF((x.X - startX) * scaleX, (x.Z - startY) * scaleY));

                            g.DrawLines(new Pen(Color.SpringGreen, 5.0f), pt.ToArray());
                        }
                    }
                    else
                    {
                        foreach (var opt in seg.Options)
                        {
                            var pt = opt.Points.Select(x => new PointF((x.X - startX) * scaleX, (x.Z - startY) * scaleY));

                            g.DrawLines(new Pen(Color.LightSkyBlue, 5.0f), pt.ToArray());
                        }
                    }
                }
            }

            if (LaneAssistance.hook != null)
            {
                var d = new PointF((LaneAssistance.hook.X - startX) * scaleX, (LaneAssistance.hook.Z - startY) * scaleY);
                g.FillEllipse(Brushes.GreenYellow, d.X - 5, d.Y - 5, 10, 10);
                var d2 = new PointF(d.X + (float)Math.Sin(LaneAssistance.yawRoad) * 25, d.Y + (float)Math.Cos(LaneAssistance.yawRoad) * 25);
                g.DrawLine(new Pen(Color.GreenYellow, 3.0f), d, d2);
            }
            if (LaneAssistance.lookPoint != null)
            {
                var d = new PointF((LaneAssistance.lookPoint.X - startX) * scaleX, (LaneAssistance.lookPoint.Y - startY) * scaleY);
                g.FillEllipse(Brushes.Pink, d.X - 5, d.Y - 5, 10, 10);
            }
            // Cities?
            var cityFont = new Font("Arial", 10.0f);
            foreach (var cities in itemsNearby.Where(x => x.Type == Ets2ItemType.City && x.StartNode!=null))
            {
                var centerX = cities.StartNode.X;
                var centerY = cities.StartNode.Z;

                var mapX = (centerX - startX)*scaleX;
                var mapY = (centerY - startY)*scaleY;
                //
                g.DrawString(cities.City, cityFont, Brushes.White, mapX,mapY);
            }

            // Draw all prefab curves
            foreach (var prefabItem in prefabs.Where(x => x.Prefab != null && x.HideUI == false).Distinct())
            {
                if (prefabItem.Prefab.Company != null)
                {
                    var nx = prefabItem.NodesList.FirstOrDefault().Value.X;
                    var ny = prefabItem.NodesList.FirstOrDefault().Value.Z;

                    var companyRect = new PointF[]
                    {
                        new PointF(nx - prefabItem.Prefab.Company.MinX, ny - prefabItem.Prefab.Company.MinY),
                        new PointF(nx - prefabItem.Prefab.Company.MinX, ny + prefabItem.Prefab.Company.MaxY),
                        new PointF(nx + prefabItem.Prefab.Company.MaxX, ny + prefabItem.Prefab.Company.MaxY),
                        new PointF(nx + prefabItem.Prefab.Company.MaxX, ny - prefabItem.Prefab.Company.MinY),
                    };

                    var offsetPoly = companyRect.Select(x => new PointF((x.X - startX) * scaleX, (x.Y - startY) * scaleY)).ToArray();

                    //g.FillPolygon(Brushes.Orange, offsetPoly);
                }
                else
                {
                    // Then it's likely a road prefab.
                    //prefab.Origin = 0;
                    // TODO: find origin
                    var originNode = prefabItem.NodesList.FirstOrDefault().Value;
                    if (originNode != null)
                    {
                        foreach (
                            var poly in
                                prefabItem.Prefab.GeneratePolygonCurves(originNode, prefabItem.Origin))
                        {
                            var offsetPoly = poly.Select(x => new PointF((x.X - startX) * scaleX, (x.Y - startY) * scaleY)).ToArray();

                            var p = new Pen(prefabLane.Color, 1.0f);
                            g.DrawLines(p, offsetPoly);
                        }
                    }
                }
            }

            var rotation = ets2Tel == null ? 0 : ets2Tel.Physics.RotationX*2*Math.PI;

            //g.FillEllipse(Brushes.Turquoise, scaleX*totalX-5, scaleY*totalY-5,10,10);
            g.DrawLine(new Pen(Brushes.Cyan, 5.0f), scaleX*totalX + (float) Math.Sin(rotation)*localPen.Width*2,
                scaleY*totalY + (float) Math.Cos(rotation)*localPen.Width*2, scaleX*totalX, scaleY*totalY);

            if (dedicated && ProcessDoubleClick)
            {
                ProcessDoubleClick = false;

                // Calculate the coordinate
                var clkX = 2 * (-0.5f + DoubleClickPoint.X / (float)clip.Width) * totalX + tx;
                var clkY = 2 * (-0.5f + DoubleClickPoint.Y / (float)clip.Height) * totalY + ty;

                var currentLocation = GetLivePoint();
                // Navigate to this point
                route = map.NavigateTo(currentLocation.Item1, new PointF(clkX, clkY));
            }

            // At 50 speed: 20fps
            // At 100 speed: 2fps
            var fps = 15.0f;
            if (ets2Tel != null && ets2Tel.Drivetrain.SpeedKmh > 50)
                fps -= (ets2Tel.Drivetrain.SpeedKmh - 50) * 0.43f; // 2.777spd = -1fps
            if (fps < 2) fps = 2;

            var interval = 1000.0f/fps;

            return (int) interval;
        }
    }
}