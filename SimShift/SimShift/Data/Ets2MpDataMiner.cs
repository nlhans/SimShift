using System;
using System.Diagnostics;
using System.Timers;
using SimShift.Data.Common;
using SimTelemetry.Domain.Memory;
using MemoryReader = SimShift.Data.Common.MemoryReader;

namespace SimShift.Data
{
    public class Ets2MpDataMiner : IDataMiner
    {
        public string Application
        {
            get { return "eurotrucks2"; }
        }

        public string Name
        {
            get { return "Euro Truck Simulator 2 - Multiplayer"; }
        }

        public bool Running { get; set; }
        public bool IsActive { get; set; }
        public bool RunEvent { get; set; }
        public bool SelectManually { get { return true; } }
        public Process ActiveProcess { get; set; }


        public EventHandler DataReceived { get; set; }
        public IDataDefinition Telemetry { get; private set; }

        private Timer _updateTel;
        private MemoryReader _ets2Reader;

        public bool SupportsCar { get { return false; } }
        public bool TransmissionSupportsRanges { get { return true; } }
        public bool EnableWeirdAntistall { get { return true; } }
        public double Weight { get { return 4500; } }

        private SimTelemetry.Domain.Memory.MemoryReader reader;
        private MemoryProvider provider;

        private int basePtr;
        private int carPtr;
        private int rpmPtr;
        private int spdPtrPtr;
        private int spdPtr;
        private int gearPtr;

        public Ets2MpDataMiner()
        {
            _updateTel = new Timer();
            _updateTel.Interval = 25;
            _updateTel.Elapsed += _updateTel_Elapsed;

            Telemetry = default(GenericDataDefinition);

            reader = new SimTelemetry.Domain.Memory.MemoryReader();
            reader.Open(Process.GetProcessesByName("flux")[0]);

            provider = new MemoryProvider(reader);
            provider.Scanner.Enable(@"C:\Users\Desktop\Documents\New folder\eurotrucks2 1.10.exe");
            
            basePtr = provider.Scanner.Scan<int>(MemoryRegionType.READ, "A1????????33C93BC6"); // OK: 1 
            carPtr = provider.Scanner.Scan<int>(MemoryRegionType.READ, "8B87????????85C074108B80"); // 8F8
            rpmPtr = provider.Scanner.Scan<int>(MemoryRegionType.READ, "D99E????????83C408C3"); // RPM; 514

            spdPtrPtr = provider.Scanner.Scan<int>(MemoryRegionType.READ, "8B48??85C974XX8B01"); // Speed 28
            spdPtr = provider.Scanner.Scan<int>(MemoryRegionType.READ, "8B4128F30F1080????????F30F5905"); // Speed 17C
            gearPtr = provider.Scanner.Scan<int>(MemoryRegionType.READ, "8B4424XX83B8????????0074XX84DB"); // Gear 67C

            // Base
            // RPM: rpmPtr -> carPtr -> base
            // Speed: spdPtr -> spdPtrPtr -> carPtr -> base
            // Gear: gearPtr -> spdPtrPtr -> carPtr -> base


        }

        public void EvtStart()
        {
            Telemetry = default(GenericDataDefinition);

            _ets2Reader = new MemoryReader();
            _ets2Reader.ReadProcess = ActiveProcess;
            _ets2Reader.Open();

            _updateTel.Start();
        }

        public void EvtStop()
        {
            _ets2Reader.Close();
            _ets2Reader = null;

            _updateTel.Stop();

            Telemetry = default(GenericDataDefinition);
        }

        public void Write<T>(TelemetryChannel channel, T i)
        {

        }

        private IntPtr GetWriteAddress(TelemetryChannel channel)
        {
            switch (channel)
            {
                default:
                    return ActiveProcess.MainModule.BaseAddress;
            }
        }

        void _updateTel_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_ets2Reader == null || _updateTel.Enabled == false)
                return;
            //
            try
            {
                if (ActiveProcess == null) return;
                var b = ActiveProcess.MainModule.BaseAddress;

                var playerBase = _ets2Reader.ReadInt32((IntPtr) (basePtr-0x400000 + b.ToInt32()));

                var rpmBase = _ets2Reader.ReadInt32(playerBase + carPtr);
                var rpm = _ets2Reader.ReadFloat(rpmBase + rpmPtr);

                var speedBase = _ets2Reader.ReadInt32(rpmBase + spdPtrPtr);
                var speed = _ets2Reader.ReadFloat(speedBase + spdPtr);

                var gearBase = _ets2Reader.ReadInt32(rpmBase + spdPtrPtr);
                var gear = _ets2Reader.ReadInt32(gearBase + gearPtr);
                
                //Debug.WriteLine(gear);
                var car = string.Empty;

                var gears = 12;

                var throttle = 0;
                var brake = 0;
                var time =
                    (float)
                    (DateTime.Now.Subtract(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0))
                         .TotalMilliseconds / 1000.0);//
                var paused = false;
                var fuel = 0;

                Telemetry = new GenericDataDefinition(car, time, paused, gear, gears, rpm, fuel, throttle, brake,
                                                      speed);

                if (DataReceived != null)
                    DataReceived(this, new EventArgs());
            }
            catch
            {
                Debug.WriteLine("Data abort error");
            }

        }
    }
}