using System;
using System.Collections.Generic;
using SimShift.Data;
using SimShift.Data.Common;

namespace SimShift.Services
{
    /// <summary>
    /// This is an budget TrackIR feature for TDU2, designed to look side-ways when a look-around stick is moved.
    /// The look side-ways auto centers. Control of camera angle is done with ProcessMemoryWrite access.
    /// </summary>
    public class CameraHorizon : IControlChainObj
    {
        public double CameraAngle = 0;
        public bool CameraHackEnabled { get; set; }

        public IEnumerable<string> SimulatorsOnly { get { return new String[] {"TestDrive2"}; } }
        public IEnumerable<string> SimulatorsBan { get { return new String[0]; } }

        public bool Active { get { return Math.Abs(CameraAngle) > 0.05; } }
        public bool Enabled { get; private set; }

        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                case JoyControls.CameraHorizon:
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
            return val;
        }

        public void TickControls()
        {
            CameraAngle = Main.GetAxisIn(JoyControls.CameraHorizon)*0.1 + CameraAngle*0.9;
        }

        public void TickTelemetry(IDataMiner data)
        {
            // TODO: Only supports TDU2.
            if (Main.Data.Active.Application != "TestDrive2")
            {
                Enabled = false;
                return;
            }
            else
            {
                Enabled = true;
            }
            if (CameraHackEnabled)
            {
                data.Write(TelemetryChannel.CameraHorizon, (float)(CameraAngle * CameraAngle * CameraAngle * -25));
                
            } else if (CameraAngle != 0)
            {
                CameraAngle = 0;
                data.Write(TelemetryChannel.CameraHorizon, 0.0f);
            }
        }

        #endregion
    }
}