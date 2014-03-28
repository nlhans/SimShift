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

        public IDataMiner Active { get; private set; }

        public IDataDefinition Telemetry { get; private set; }

        public int verbose = 0;

        public event EventHandler AppActive;
        public event EventHandler AppInactive;

        public event EventHandler DataReceived;

        private Timer _checkApplications;

        public DataArbiter()
        {
            miners.Add(new Ets2DataMiner());
            miners.Add(new Tdu2DataMiner());

            foreach(var app in miners)
            {
                app.DataReceived += (s, e) =>
                                        {
                                            if (app == Active)
                                            {
                                                if(verbose>0)
                                                Debug.WriteLine(
                                                    string.Format(
                                                        "[Data] Spd: {0:000.0}kmh Gear: {1} RPM: {2:0000}rpm Throttle: {3:0.000}",
                                                        app.Telemetry.Speed, app.Telemetry.Gear, app.Telemetry.EngineRpm,
                                                        app.Telemetry.Throttle));
                                                Telemetry = app.Telemetry;
                                                if (DataReceived != null)
                                                    DataReceived(s, e);
                                            }
                                        };
            }

            _checkApplications = new Timer();
            _checkApplications.Interval = 1000;
            _checkApplications.Elapsed += _checkApplications_Elapsed;
            _checkApplications.Start();
        }

        void _checkApplications_Elapsed(object sender, ElapsedEventArgs e)
        {
            var prcsList = Process.GetProcesses();

            foreach (var app in miners)
            {
                bool wasRuning = app.Running;
                app.Running = prcsList.Any(x => x.ProcessName.ToLower() == app.Application.ToLower());
                app.RunEvent = app.Running != wasRuning;
                app.ActiveProcess = prcsList.FirstOrDefault(x => x.ProcessName.ToLower() == app.Application.ToLower());

                if (app.RunEvent && app.IsActive && app.Running==false)
                {
                    app.EvtStop();
                    if (AppInactive != null)
                        AppInactive(this, new EventArgs());
                }

                app.IsActive = false;

            }
            if (miners.Any(x => x.Running))
            {
                // Conflict?
                Active = miners.Count(x => x.Running) != 1 ? null : miners.FirstOrDefault(x => x.Running);
                Active.IsActive = true;
                
                // TODO: This seems buggy way..
                if(Active.RunEvent)
                {
                    Active.EvtStart();
                    if (AppActive != null)
                        AppActive(this, new EventArgs());
                }
            }
        }
    }
}
