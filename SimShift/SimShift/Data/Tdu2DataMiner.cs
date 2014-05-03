using System;
using System.Diagnostics;
using System.Timers;
using SimShift.Data.Common;

namespace SimShift.Data
{
    public class Tdu2DataMiner : IDataMiner
    {
        public string Application
        {
            get { return "TestDrive2"; }
        }

        public bool Running { get; set; }
        public bool IsActive { get; set; }
        public bool RunEvent { get; set; }
        public Process ActiveProcess { get; set; }

        public EventHandler DataReceived { get; set; }
        public IDataDefinition Telemetry { get; private set; }

        private Timer _updateTel;
        private MemoryReader _tdu2Reader;

        public bool TransmissionSupportsRanges { get { return false; } }
        public bool EnableWeirdAntistall { get { return false; } }
        public double Weight { get { return 1500; } }

        // Enable write operations?
        bool openedTduAsWriter;

        public Tdu2DataMiner()
        {
            _updateTel = new Timer();
            _updateTel.Interval = 25;
            _updateTel.Elapsed += _updateTel_Elapsed;

            Telemetry = default(GenericDataDefinition);
        }

        public void EvtStart()
        {
            Telemetry = default(GenericDataDefinition);

            _tdu2Reader = new MemoryReader();
            _tdu2Reader.ReadProcess = ActiveProcess;
            _tdu2Reader.Open();
            openedTduAsWriter = false;

            _updateTel.Start();
        }

        public void EvtStop()
        {
            _tdu2Reader.Close();
            _tdu2Reader = null;

            _updateTel.Stop();

            Telemetry = default(GenericDataDefinition);
        }

        public void Write<T>(TelemetryChannel channel, T i)
        {
            if (ActiveProcess == null)
                return;

            if (!openedTduAsWriter)
            {
                _tdu2Reader.Close();
                _tdu2Reader = new MemoryWriter();
                _tdu2Reader.ReadProcess = ActiveProcess;
                _tdu2Reader.Open();

                openedTduAsWriter = true;
            }

            var channelAddress = GetWriteAddress(channel);

            var writer = _tdu2Reader as MemoryWriter;
            if (i is float)
                writer.WriteFloat(channelAddress, float.Parse(i.ToString()));
           
        }

        private IntPtr GetWriteAddress(TelemetryChannel channel)
        {
            switch (channel)
            {
                case TelemetryChannel.CameraHorizon:
                    return GetWriteAddress(TelemetryChannel.CameraViewBase) + 0x550;

                case TelemetryChannel.CameraViewBase:
                    return (IntPtr) _tdu2Reader.ReadInt32(ActiveProcess.MainModule.BaseAddress + 0xD95BF0);

                default:
                    return ActiveProcess.MainModule.BaseAddress;
            }
        }

        void _updateTel_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_tdu2Reader == null || _updateTel.Enabled == false)
                return;
            //
            try
            {
                var b = ActiveProcess.MainModule.BaseAddress;

                var car = _tdu2Reader.ReadString(b + 0xC2DC30, 32);
                var gear = _tdu2Reader.ReadInt32(b + 0xC2DAD0) - 1;
                var gears = 7;
                var speed = _tdu2Reader.ReadFloat(b + 0xC2DB24)/3.6f;
                var throttle = _tdu2Reader.ReadFloat(b + 0xC2DB00);
                var brake = _tdu2Reader.ReadFloat(b + 0xC2DB04);
                var time =
                    (float)
                    (DateTime.Now.Subtract(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0))
                         .TotalMilliseconds/1000.0);
                var paused = false;
                var engineRpm = _tdu2Reader.ReadFloat(b + 0xC2DB18);
                var fuel = 0;

                Telemetry = new GenericDataDefinition(car, time, paused, gear, gears, engineRpm, fuel, throttle, brake,
                                                      speed);

                if (DataReceived != null)
                    DataReceived(this, new EventArgs());
            }catch
            {
                Debug.WriteLine("Data abort error");
            }

        }
    }
}