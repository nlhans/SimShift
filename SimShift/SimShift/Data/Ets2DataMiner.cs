using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Ets2SdkClient;
using SimShift.Entities;
using SimTelemetry.Domain.Memory;

namespace SimShift.Data
{
    public class Ets2DataMiner : SimShift.Data.Common.IDataMiner
    {
        public string Application
        {
            get { return "eurotrucks2"; }
        }

        public string Name
        {
            get { return "Euro Truck Simulator 2"; }
        }

        public bool Running { get; set; }
        public bool IsActive { get; set; }
        public bool RunEvent { get; set; }

        public bool SelectManually
        {
            get { return false; }
        }

        private Process _ap;

        public Process ActiveProcess
        {
            get { return _ap; }
            set
            {
                _ap = value;
                if (miner == null && _ap != null)
                AdvancedMiner();
            }
        }

        public bool SupportsCar
        {
            get { return true; }
        }

        public bool TransmissionSupportsRanges
        {
            get { return true; }
        }

        public bool EnableWeirdAntistall
        {
            get { return true; }
        }

        public void EvtStart()
        {
            _telemetryUpdater.Start();
        }

        public void EvtStop()
        {
            _telemetryUpdater.Stop();
        }

        public void Write<T>(TelemetryChannel cameraHorizon, T i)
        {
            // Not supported
        }

        public EventHandler DataReceived { get; set; }
        private Ets2SdkTelemetry sdktel;

        private MemoryProvider miner;

        public List<Ets2Car> Cars = new List<Ets2Car>(); 

        public Ets2Telemetry MyTelemetry { get; private set; }
        public SimShift.Data.Common.IDataDefinition Telemetry { get; private set; }

        /*** MyTelemetry data source & update control ***/
        private readonly MmTimer _telemetryUpdater = new MmTimer(10);
        private Socket server;
        public Ets2DataMiner()
        {
        
                 server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            sdktel = new Ets2SdkTelemetry(10);
            sdktel.Data += (data, timestamp) =>
            {
                MyTelemetry = data;

                var veh = (data.TruckId.StartsWith("vehicle.") ? data.TruckId.Substring(8) : data.TruckId);
                var newData = new SimShift.Data.Common.GenericDataDefinition(veh, data.Time / 1000000.0f, data.Paused, data.Drivetrain.Gear,
                    data.Drivetrain.GearsForward, data.Drivetrain.EngineRpm, data.Drivetrain.Fuel,
                    data.Controls.GameThrottle, data.Controls.GameBrake, data.Drivetrain.Speed);
                Telemetry = newData;
                if (DataReceived != null)
                    DataReceived(this, new EventArgs());
            };
            sdktel.Data += sdktel_Data;
        }

        private void AdvancedMiner()
        {
            var reader = new MemoryReader();
            reader.Open(ActiveProcess, true);
            miner = new MemoryProvider(reader);

            var scanner = new MemorySignatureScanner(reader);
            scanner.Enable();
            var staticAddr = scanner.Scan<int>(MemoryRegionType.READWRITE, "75E98B0D????????5F5E");
            var staticOffset = scanner.Scan<byte>(MemoryRegionType.READWRITE, "578B7E??8D04973BF8742F");
            var ptr1Offset = 0;
            var spdOffset = scanner.Scan<byte>(MemoryRegionType.READWRITE, "DEC9D947??DECADEC1D955FC");
            var cxOffset = scanner.Scan<byte>(MemoryRegionType.READWRITE, "F30F5C4E??F30F59C0F30F59");
            var cyOffset = cxOffset + 4;// scanner.Scan<byte>(MemoryRegionType.READWRITE, "5F8B0A890E8B4A??894EXX8B4AXX894EXX");
            var czOffset = cxOffset + 8;// scanner.Scan<byte>(MemoryRegionType.READWRITE, "8B4A08??894EXXD9420CD95E0C");
            scanner.Disable();

            var carsPool = new MemoryPool("Cars", MemoryAddress.StaticAbsolute, staticAddr, new int[] { 0, staticOffset}, 64*4);

            miner.Add(carsPool);

            for (int k = 0; k < 64; k++)
            {
                var carPool = new MemoryPool("Car " + k, MemoryAddress.Dynamic, carsPool, k * 4, 512);
                carPool.Add(new MemoryField<float>("Speed", MemoryAddress.Dynamic, carPool, spdOffset, 4));
                carPool.Add(new MemoryField<float>("CoordinateX", MemoryAddress.Dynamic, carPool, cxOffset, 4));
                carPool.Add(new MemoryField<float>("CoordinateY", MemoryAddress.Dynamic, carPool, cyOffset, 4));
                carPool.Add(new MemoryField<float>("CoordinateZ", MemoryAddress.Dynamic, carPool, czOffset, 4));

                miner.Add(carPool);

                Cars.Add(new Ets2Car {ID = k});
            }
        }

        private bool sdkBusy=false;
        private void sdktel_Data(Ets2Telemetry data, bool newTimestamp)
        {
            if (sdkBusy) return;
            sdkBusy = true;
            try
            {
                if (miner != null)
                {
                    miner.Refresh();
                    for (int k = 0; k < 64; k++)
                    {
                        var carPool = miner.Get("Car " + k);
                        if (carPool == null) continue;
                        var car = Cars.FirstOrDefault(x => x.ID == k);
                        if (car == null) continue;

                        car.Speed = carPool.ReadAs<float>("Speed");
                        car.X = carPool.ReadAs<float>("CoordinateX");
                        car.Y = carPool.ReadAs<float>("CoordinateY");
                        car.Z = carPool.ReadAs<float>("CoordinateZ");
                    }
                }

                var ep = new IPEndPoint(IPAddress.Parse("192.168.1.158"), 12345);
                var r = (data.Drivetrain.EngineRpm - 300)/(2500 - 300);
                if (data.Drivetrain.EngineRpm < 300) r = -1;
                var s = ((int) (r*10000)).ToString() + "," +
                        ((int) (data.Controls.GameThrottle*1000)).ToString() + "," + ((data.Paused) ? 1 : 0);
                var sb = ASCIIEncoding.ASCII.GetBytes(s);
                var dgram = ASCIIEncoding.ASCII.GetBytes(s);

                server.SendTo(dgram, ep);
            }
            catch
            {
                
            }
            sdkBusy = false;
        }
    }

    public class Ets2Car
    {
        public int ID;

        public bool Tracked;

        public float Speed;

        public float X;
        public float Y;
        public float Z;

        public float Heading;

        public float Length;

        private float lastX = 0.0f;
        private float lastY = 0.0f;

        public PointF[] Box;

        public bool Valid
        {
            get
            {
                if (Box == null || Math.Abs(Speed) > 200 || Math.Abs(X) > 1E7 || Math.Abs(Z) > 1E7 || float.IsNaN(X) ||
                    float.IsNaN(Z) || float.IsInfinity(X) || float.IsInfinity(Z))
                    return false;
                else
                    return true;

            }
        }

        public float Distance;
        public float TTI;

        public void Tick()
        {
            var dx = X - lastX;
            var dy = Z - lastY;
            if (Math.Abs(dx) >= 0.02f || Math.Abs(dy) >= 0.02f)
            {
                Heading = (float) (Math.PI - Math.Atan2(dy, dx));

            }
            // Rotated polygon
            var carL = 12.0f;
            var carW = 3.0f;
            var hg = -Heading; //
            Box = new PointF[]
                {
                    new PointF(X + carL/2*(float) Math.Cos(hg) - carW/2*(float) Math.Sin(hg),
                        Z + carL/2*(float) Math.Sin(hg) + carW/2*(float) Math.Cos(hg)),
                    new PointF(X - carL/2*(float) Math.Cos(hg) - carW/2*(float) Math.Sin(hg),
                        Z - carL/2*(float) Math.Sin(hg) + carW/2*(float) Math.Cos(hg)),
                    new PointF(X - carL/2*(float) Math.Cos(hg) + carW/2*(float) Math.Sin(hg),
                        Z - carL/2*(float) Math.Sin(hg) - carW/2*(float) Math.Cos(hg)),
                    new PointF(X + carL/2*(float) Math.Cos(hg) + carW/2*(float) Math.Sin(hg),
                        Z + carL/2*(float) Math.Sin(hg) - carW/2*(float) Math.Cos(hg)),
                };



            lastX = X;
            lastY = Z;
        }
    }
}