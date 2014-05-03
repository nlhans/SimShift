using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        public bool TransmissionSupportsRanges { get { return true; } }
        public bool EnableWeirdAntistall { get { return true; } }
        public double Weight { get; private set; }

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

        public bool NewFuelFlow { get; private set; }
        public float FuelFlow { get; private set; }
        public IDataDefinition Telemetry { get; private set; }
        public Ets2DataDefinition MyTelemetry { get; private set; }

        public string Truck { get; private set; }
        public string Trailer { get; private set; }
        public string TrailerName { get; private set; }
        public string TrailerTonnage { get; private set; }

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
            ParseTrailerFiles();
        }

        private void UpdateTelemetry(object sender, ElapsedEventArgs args)
        {
            _sharedMem.Update();
            MyTelemetry = _sharedMem.Data;

            // read ID
            if (MyTelemetry.modelLength > 0)
            {
                var id = ASCIIEncoding.ASCII.GetString(_sharedMem.RawData, MyTelemetry.modelOffset, MyTelemetry.modelLength);

                var prevTruck = Truck;
                Truck = id.Substring("vehicle.".Length);
                if (prevTruck != Truck)
                    Debug.WriteLine("New Truck: " + Truck);
            }
            if (MyTelemetry.trailerLength > 0)
            {
                var id = ASCIIEncoding.ASCII.GetString(_sharedMem.RawData, MyTelemetry.trailerOffset, MyTelemetry.trailerLength);

                var prevTrrailer = Trailer;
                Trailer = id.Substring("cargo.".Length);
                if (prevTrrailer != Trailer)
                {
                    Weight = 9650 + LookupTrailerWeight(Trailer);
                    Debug.WriteLine("New Trailer: " + Trailer);
                }
            }
            Telemetry = MyTelemetry.ToGeneric(Truck);

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

        private Dictionary<string, KeyValuePair<string, double>> trailerWeights = new Dictionary<string, KeyValuePair<string, double>>(); 

        private void ParseTrailerFiles()
        {
            string[] trailers = Directory.GetFiles("./Cargo/");

            foreach(var t in trailers)
            {
                var r = ParseTrailerFile(t);
                if(!trailerWeights.ContainsKey(r.Key))
                    trailerWeights.Add(r.Key, r.Value);
            }
        }

        private KeyValuePair<string, KeyValuePair<string,double>> ParseTrailerFile(string trailer)
        {
            string[] l = File.ReadAllLines(trailer);
            var trailerWeight = 20000.0;
            var name = "?";
            var vehicle = "";
            foreach (var line in l)
            {
                var ls = line.Trim();
                if (ls.StartsWith("name:"))
                {
                    name = ls.Replace("name: ", "");
                    if (name.Length > 2)
                        name = name.Substring(1, name.Length - 2);
                }
                if (ls.StartsWith("mass"))
                {
                    var tmp = ls.Substring(ls.IndexOf(" "));
                    trailerWeight = double.Parse(tmp);

                }
                if (ls.StartsWith("vehicles"))
                {
                    var tmp = ls.Substring(ls.IndexOf(" "));
                    if (tmp.Length > 2)
                        vehicle = tmp.Substring(1, tmp.Length - 1);

                }
            }
            vehicle = vehicle.Replace("trailer.", "");
            return new KeyValuePair<string, KeyValuePair<string, double>>(vehicle, new KeyValuePair<string, double>(name, trailerWeight));
        }

        private double LookupTrailerWeight(string trailer)
        {
            Debug.WriteLine("Looking up trailer " + trailer);

            if (trailerWeights.ContainsKey(trailer))
            {
                TrailerName = trailerWeights[trailer].Key;
                TrailerTonnage = string.Format("{0:0.0}t", trailerWeights[trailer].Value / 1000.0);
                return trailerWeights[trailer].Value;
            }else if (trailerWeights.Any(x=>x.Key.StartsWith(trailer)))
            {
                var t = trailerWeights.Where(x => x.Key.StartsWith(trailer));
                if (t.Any())
                {
                    var tr = t.FirstOrDefault();
                    TrailerName = tr.Value.Key;
                    TrailerTonnage = string.Format("{0:00.0}t", tr.Value.Value/1000.0);
                    return tr.Value.Value;
                }
            }

                Debug.WriteLine("Not found, assuming 20t");
                TrailerName = "?";
                TrailerTonnage = "20t?";
                return 20000;
                
            
        }
    }
}
