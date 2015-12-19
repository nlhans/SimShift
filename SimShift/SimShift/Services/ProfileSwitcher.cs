using System;
using System.Collections.Generic;
using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    /// This module monitors user inputs to switch driving profiles.
    /// Driving profiles contain a set of settings for all different driving modules, e.g. transmission, speedlimiter, traction control, etc.
    /// </summary>
    public class ProfileSwitcher : IControlChainObj
    {
        public bool Active { get { return ProfileSwitchFrozen; } }
        public bool Enabled { get { return true; } }

        public IEnumerable<string> SimulatorsOnly { get { return new String[0]; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        //

        public DateTime ProfileSwitchTimeout { get; private set; }
        public bool ProfileSwitchFrozen { get { return ProfileSwitchTimeout > DateTime.Now; } }

        public DateTime TransmissionReverseTimeout { get; private set; }
        public bool TransmissionReverseFrozen { get { return TransmissionReverseTimeout > DateTime.Now; } }

        private bool TrailerAttached = false;

        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.GearUp:
                case JoyControls.GearDown:
                    return true;

                default:
                    return false;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            return val;
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch(c)
            {
                case JoyControls.GearUp:
                    if (val && !ProfileSwitchFrozen)
                    {
                        ProfileSwitchTimeout = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
                        if (Main.Data.Active.Application == "eurotrucks2")
                        {
                            //
                            var ets2miner = (Ets2DataMiner) Main.Data.Active;
                            var ets2telemetry = ets2miner.MyTelemetry;
                            Main.LoadNextProfile(ets2telemetry.Job.Mass);
                        }
                        else
                        {
                            Main.LoadNextProfile(10000);
                        }
                    }
                    return false;
                    break;
                case JoyControls.GearDown:
                    if(val && !TransmissionReverseFrozen)
                    {
                        TransmissionReverseTimeout = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
                        Transmission.InReverse = !Transmission.InReverse;
                    }
                    return false;
                    break;

                default:
                    return val;

            }
        }

        public void TickControls()
        {
            //
        }

        public void TickTelemetry(IDataMiner data)
        {
            //
            if (data.Application == "eurotrucks2")
            {
                //
                var ets2miner = (Ets2DataMiner) data;
                var ets2telemetry = ets2miner.MyTelemetry;
                var trailerAttached = ets2telemetry.Job.TrailerAttached;
                if (trailerAttached != TrailerAttached)
                {
                    TrailerAttached = trailerAttached;
                    Main.ReloadProfile(trailerAttached ? ets2telemetry.Job.Mass : 0);
                }
            }
        }

        #endregion
    }
}