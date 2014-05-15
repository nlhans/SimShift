using System;
using System.Diagnostics;
using System.Timers;
using SimShift.Data.Common;

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

        public Ets2MpDataMiner()
        {
            _updateTel = new Timer();
            _updateTel.Interval = 25;
            _updateTel.Elapsed += _updateTel_Elapsed;

            Telemetry = default(GenericDataDefinition);
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

                var playerBase = _ets2Reader.ReadInt32(b + 0x8116EC);

                var rpmBase = _ets2Reader.ReadInt32(playerBase + 0x8F8);
                var rpm = _ets2Reader.ReadFloat(rpmBase + 0x78);

                var speedBase = _ets2Reader.ReadInt32(rpmBase + 0x28);
                var speed = _ets2Reader.ReadFloat(speedBase + 0x17C);

                var gearBase = _ets2Reader.ReadInt32(rpmBase + 0x28);
                var gear = _ets2Reader.ReadInt32(gearBase + 0x67C);
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