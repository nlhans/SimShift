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

            _updateTel.Start();
        }

        public void EvtStop()
        {
            _tdu2Reader.Close();
            _tdu2Reader = null;

            _updateTel.Stop();

            Telemetry = default(GenericDataDefinition);
        }

        void _updateTel_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_tdu2Reader == null || _updateTel.Enabled == false)
                return;
            //
            var b = ActiveProcess.MainModule.BaseAddress;

            var gear = _tdu2Reader.ReadInt32(b + 0xC2DAD0);
            var gears = 7;
            var speed = _tdu2Reader.ReadFloat(b + 0xC2DB24);
            var throttle = _tdu2Reader.ReadFloat(b+ 0xC2DB00);
            var brake = _tdu2Reader.ReadFloat(b + 0xC2DB04);
            var time = (float) (DateTime.Now.Subtract(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,0,0,0)).TotalMilliseconds/1000.0);
            var paused = false;
            var engineRpm = _tdu2Reader.ReadFloat(b + 0xC2DB18);
            var fuel = 0;

            Telemetry = new GenericDataDefinition(time, paused, gear, gears, engineRpm, fuel, throttle, brake, speed);

            if(DataReceived!=null)
                DataReceived(this, new EventArgs());
        }
    }
}