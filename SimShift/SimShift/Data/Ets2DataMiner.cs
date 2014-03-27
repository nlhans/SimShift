using System;
using System.Diagnostics;
using System.Timers;

namespace SimShift.Data
{
    public class Ets2DataMiner
    {
        public EventHandler DataReceived;

        public bool NewFuelFlow { get; private set; }
        public float FuelFlow { get; private set; }
        public Ets2DataDefinition Telemetry { get; private set; }

        /*** Telemetry data source & update control ***/
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
            Telemetry = default(Ets2DataDefinition);

            _telemetryUpdater.Elapsed += UpdateTelemetry;
            _telemetryUpdater.Start();
        }

        private void UpdateTelemetry(object sender, ElapsedEventArgs args)
        {
            _sharedMem.Update();
            Telemetry = _sharedMem.Data;

            // Compute new fuel flow, based on time stamp difference.
            // Unfortunately this implementation doesn't allow for 0.0L/h output; as there is no fuel burned
            // TODO: upgrade this
            uint dt = Telemetry.time - _previousTimestamp;
            if (dt > 0 && Math.Abs(_previousFuel - Telemetry.fuel) > 0.02)
            {
                FuelFlow = _previousFuel - Telemetry.fuel;
                FuelFlow = FuelFlow/(dt/1000000.0f)*3600/3f;

                _previousFuel = Telemetry.fuel;
                _previousTimestamp = Telemetry.time;

                NewFuelFlow = true;
            }
            else
            {
                NewFuelFlow = false;
            }
            //Debug.WriteLine(string.Format("CX: {3:0000.0} CY: {4:0000.0} CZ: {5:0000.0} RX: {0:00.00000} RY: {1:00.00000} RZ: {2:00.00000}", Telemetry.rotationX, Telemetry.rotationY, Telemetry.rotationZ,
            //    Telemetry.coordinateX,Telemetry.coordinateY,Telemetry.coordinateZ));
            if (DataReceived != null)
                DataReceived(this, new EventArgs());
        }

    }
}
