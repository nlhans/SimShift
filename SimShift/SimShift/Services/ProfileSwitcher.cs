using System;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public class ProfileSwitcher : IControlChainObj
    {
        public bool Active { get { return ProfileSwitchFrozen; } }
        public bool Enabled { get { return true; } }

        public DateTime ProfileSwitchTimeout { get; private set; }
        public bool ProfileSwitchFrozen { get { return ProfileSwitchTimeout > DateTime.Now; } }

        public DateTime TransmissionReverseTimeout { get; private set; }
        public bool TransmissionReverseFrozen { get { return TransmissionReverseTimeout > DateTime.Now; } }

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
                        Main.LoadNextProfile();
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
        }

        #endregion
    }
}