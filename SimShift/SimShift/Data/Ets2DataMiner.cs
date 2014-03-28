using System;
using System.Diagnostics;
using System.Timers;
using SimShift.Data.Common;

namespace SimShift.Data
{
    public class Ets2DataMiner : IDataMiner
    {
        public string Application { get { return "eurotrucks2"; } }

        public bool Running { get; set; }
        public bool IsActive { get; set; }
        public bool RunEvent { get; set; }
        public Process ActiveProcess { get; set; }
        public void EvtStart()
        {
            _telemetryUpdater.Start();
        }

        public void EvtStop()
        {
            _telemetryUpdater.Stop();
        }

        public EventHandler DataReceived { get; set; }

        public bool NewFuelFlow { get; private set; }
        public float FuelFlow { get; private set; }
        public IDataDefinition Telemetry { get; private set; }
        public Ets2DataDefinition MyTelemetry { get; private set; }

        /*** MyTelemetry data source & update control ***/
        private readonly SharedMemory<Ets2DataDefinition> _sharedMem = new SharedMemory<Ets2DataDefinition>();
        private readonly Timer _telemetryUpdater = new Timer { Interval = 25 };

        /*** Required for computing fuel flow ***/
        private float _previousFuel;
        private uint _previousTimestamp;
        
        public Ets2DataMiner()
        {
            _sharedMem.Connect(@"Local\SimTelemetryETS2");

            NewFuelFlow = false;
            FuelFlow = 0;
            MyTelemetry = default(Ets2DataDefinition);

            _telemetryUpdater.Elapsed += UpdateTelemetry;
        }

        private void UpdateTelemetry(object sender, ElapsedEventArgs args)
        {
            _sharedMem.Update();
            MyTelemetry = _sharedMem.Data;
            Telemetry = MyTelemetry.ToGeneric();

            // Compute new fuel flow, based on time stamp difference.
            // Unfortunately this implementation doesn't allow for 0.0L/h output; as there is no fuel burned
            // TODO: upgrade this
            uint dt = MyTelemetry.time - _previousTimestamp;
            if (dt > 0 && Math.Abs(_previousFuel - MyTelemetry.fuel) > 0.02)
            {
                FuelFlow = _previousFuel - MyTelemetry.fuel;
                FuelFlow = FuelFlow/(dt/1000000.0f)*3600/3f;

                _previousFuel = MyTelemetry.fuel;
                _previousTimestamp = MyTelemetry.time;

                NewFuelFlow = true;
            }
            else
            {
                NewFuelFlow = false;
            }
            //Debug.WriteLine(string.Format("CX: {3:0000.0} CY: {4:0000.0} CZ: {5:0000.0} RX: {0:00.00000} RY: {1:00.00000} RZ: {2:00.00000}", MyTelemetry.rotationX, MyTelemetry.rotationY, MyTelemetry.rotationZ,
            //    MyTelemetry.coordinateX,MyTelemetry.coordinateY,MyTelemetry.coordinateZ));
            if (DataReceived != null)
                DataReceived(this, new EventArgs());
        }

    }
}
