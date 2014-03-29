using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SimShift.Data.Common;
using SimShift.Utils;

namespace SimShift.Services
{
    public class TractionControl : IControlChainObj, IConfigurable
    {
        public bool Slipping { get; private set; }
        public double WheelSpeed { get; private set; }

        #region Implementation of IControlChainObj

        public bool Requires(JoyControls c)
        {
            switch(c)
            {
                default:
                    return false;

                case JoyControls.Throttle:
                    return Slipping;
            }
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                default:
                    return val;

                case JoyControls.Throttle:
                    return val * (1- (WheelSpeed - AllowedSlip)/Slope);
                    break;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public void TickControls()
        {
            //
        }

        public void TickTelemetry(IDataMiner data)
        {
            try
            {
                WheelSpeed = Main.Drivetrain.CalculateSpeedForRpm(data.Telemetry.Gear - 1, data.Telemetry.EngineRpm);
                if (double.IsInfinity(WheelSpeed)) WheelSpeed = 0;
                if (Main.Antistall.Stalling) WheelSpeed = 0;
                Slipping = (WheelSpeed - AllowedSlip > data.Telemetry.Speed);
            }catch
            {
            }
        }

        #endregion

        #region Implementation of IConfigurable

        public double AllowedSlip { get; private set; }
        public double Slope { get; private set; }

        public IEnumerable<string> AcceptsConfigs { get { return new string[] {"TractionControl"}; } }
        public void ResetParameters()
        {
            AllowedSlip = 5;
            Slope = 5;
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "Slope":
                    Slope = obj.ReadAsFloat();
                    break;

                case "Slip":
                    AllowedSlip = obj.ReadAsFloat();
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();

            obj.Add(new IniValueObject(AcceptsConfigs, "Slope", Slope.ToString()));
            obj.Add(new IniValueObject(AcceptsConfigs, "Slip", AllowedSlip.ToString()));

            return obj;
        }

        #endregion
    }
}
