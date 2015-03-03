using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using SimShift.Data.Common;

namespace SimShift.Data
{
    public class DataArbiter
    {
        private readonly List<IDataMiner> miners = new List<IDataMiner>();

        public IEnumerable<IDataMiner> Miners { get { return miners; } } 

        public IDataMiner Active { get; private set; }

        public IDataDefinition Telemetry { get; private set; }

        public bool AutoMode { get; private set; }

        public string ManualCar { get; private set; }

        public int verbose = 0;

        public event EventHandler AppActive;
        public event EventHandler AppInactive;

        public event EventHandler CarChanged;
        public event EventHandler DataReceived;

        private Timer _checkApplications;

        private string lastCar;

        public DataArbiter()
        {
            AutoMode = true;

            miners.Add(new Ets2DataMiner());
            miners.Add(new Tdu2DataMiner());

            miners.ForEach(app =>
                               {

                                   app.DataReceived += (s, e) =>
                                                           {
                                                               if (app == Active)
                                                               {
                                                                   if (verbose > 0)
                                                                       Debug.WriteLine(
                                                                           string.Format(
                                                                               "[Data] Spd: {0:000.0}kmh Gear: {1} RPM: {2:0000}rpm Throttle: {3:0.000}",
                                                                               app.Telemetry.Speed, app.Telemetry.Gear,
                                                                               app.Telemetry.EngineRpm,
                                                                               app.Telemetry.Throttle));
                                                                   Telemetry = app.Telemetry;

                                                                   if (lastCar != Telemetry.Car && CarChanged != null && app.SupportsCar)
                                                                   {
                                                                       lastCar = Telemetry.Car;
                                                                       Debug.WriteLine("New car:" + Telemetry.Car); 
                                                                       CarChanged(s, e);
                                                                   }
                                                                   if(!app.SupportsCar)
                                                                   {
                                                                       Telemetry.Car = ManualCar;
                                                                   }
                                                                   if (DataReceived != null)
                                                                       DataReceived(s, e);
                                                                   lastCar = Telemetry.Car;
                                                               }
                                                           };
                               });

            _checkApplications = new Timer();
            _checkApplications.Interval = 1000;
            _checkApplications.Elapsed += _checkApplications_Elapsed;
        }

        public void AutoSelectApp()
        {
            AutoMode = true;
            if (Active != null && Active.IsActive)
            {
                if (AppInactive != null)
                    AppInactive(this, new EventArgs());
            }
            Active = null;
        }

        public void ManualSelectApp(IDataMiner app)
        {
            AutoMode = false;
            if (this.miners.Contains(app))
            {
                if (Active != null && Active.IsActive )
                {
                    if (AppInactive!=null)
                        AppInactive(this, new EventArgs());
                }
                Active = app;
            }
        }

        void _checkApplications_Elapsed(object sender, ElapsedEventArgs e)
        {
            var prcsList = Process.GetProcesses();

            // Do it for the manual selected sim
            if (!AutoMode)
            {
                var app = this.Active;
                if (app == null) return;
                // Search for the process
                bool wasRuning = app.Running;
                app.Running = prcsList.Any(x => x.ProcessName.ToLower() == app.Application.ToLower());
                app.RunEvent = app.Running != wasRuning;
                app.ActiveProcess = prcsList.FirstOrDefault(x => x.ProcessName.ToLower() == app.Application.ToLower());

                if (app.RunEvent)
                {
                    if (app.Running)
                    {
                        app.EvtStart();
                        if (AppActive != null)
                            AppActive(this, new EventArgs());
                        else
                        {
                            app.EvtStop();
                            if (AppInactive != null)
                                AppInactive(this, new EventArgs());
                        }
                    }
                }
            }
            else
            {
                foreach (var app in miners.Where(x => !x.SelectManually))
                {
                    bool wasRuning = app.Running;
                    app.Running = prcsList.Any(x => x.ProcessName.ToLower() == app.Application.ToLower());
                    app.RunEvent = app.Running != wasRuning;
                    app.ActiveProcess =
                        prcsList.FirstOrDefault(x => x.ProcessName.ToLower() == app.Application.ToLower());

                    if (app.RunEvent && app.IsActive && app.Running == false)
                    {
                        app.EvtStop();
                        if (AppInactive != null)
                            AppInactive(this, new EventArgs());
                    }

                    app.IsActive = false;

                }
                if (miners.Where(x => !x.SelectManually).Any(x => x.Running))
                {
                    // Conflict?
                    Active = miners.Count(x => x.Running) != 1 ? null : miners.Where(x => !x.SelectManually).FirstOrDefault(x => x.Running);
                    if (Active != null)
                    {
                        Active.IsActive = true;

                        // TODO: This seems buggy way..
                        if (Active.RunEvent)
                        {
                            Active.EvtStart();
                            if (AppActive != null)
                                AppActive(this, new EventArgs());
                        }
                    }
                }
            }
        }

        public void Run()
        {
            _checkApplications.Start();
        }

        public void ChangeCar(string newCar)
        {
            ManualCar = newCar;

            Debug.WriteLine("New car:" + Telemetry.Car);
            CarChanged(this, new EventArgs());

        }

    }
}
