using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SimShift.Controllers;
using SimShift.Data;
using SimShift.Models;
using SimShift.Utils;

namespace SimShift.Services
{
    public class Main
    {
        private static bool requiresSetup = true;

        private static Dictionary<JoyControls, double> AxisFeedback = new Dictionary<JoyControls, double>();
        private static Dictionary<JoyControls, bool> ButtonFeedback = new Dictionary<JoyControls, bool>(); 

        public static List<JoystickInput> RawJoysticksIn = new List<JoystickInput>();
        public static List<JoystickOutput> RawJoysticksOut = new List<JoystickOutput>();


        public static DataArbiter Data;
        public static WorldMapper Map;

        public static Profiles CarProfile;

        // Modules
        public static Antistall Antistall;
        public static CruiseControl CruiseControl;
        public static IDrivetrain Drivetrain;
        public static Speedlimiter Speedlimiter;
        public static Transmission Transmission;
        public static TractionControl TractionControl;
        public static ProfileSwitcher ProfileSwitcher;

        public static ControlChain Controls;

        public static bool Running { get; private set; }

        public static DrivetrainCalibrator DrivetrainCalibrator;

        public static void Save()
        {
            if(Map!=null)
            Map.Export();
        }

        public static void Setup()
        {
            if (requiresSetup)
            {
                requiresSetup = false;

                // Joysticks
                var ps3 = JoystickInputDevice.Search("Motion").FirstOrDefault();
                var ps3Controller = new JoystickInput(ps3);
                var vJoy = new JoystickOutput();

                RawJoysticksIn.Add(ps3Controller);
                RawJoysticksOut.Add(vJoy);

                // Data source
                Data = new DataArbiter();

                Data.CarChanged += (s, e) =>
                                       {
                                           if (Data.Active.Application == "eurotrucks2")
                                               Drivetrain = new Ets2Drivetrain();
                                           else
                                               Drivetrain = new GenericDrivetrain();

                                           // reset all modules
                                           Antistall.ResetParameters();
                                           CruiseControl.ResetParameters();
                                           Drivetrain.ResetParameters();
                                           Transmission.ResetParameters();
                                           TractionControl.ResetParameters();
                                           Speedlimiter.ResetParameters();

                                           CarProfile = new Profiles(Data.Active.Application, Data.Telemetry.Car);
                                           LoadNextProfile();
                                       };

                Data.AppActive += (s, e) => { Map = new WorldMapper(Data.Active); };
                Data.AppInactive += (s, e) => { Map = null; };

                // Modules
                Antistall = new Antistall();
                CruiseControl = new CruiseControl();
                Drivetrain = new GenericDrivetrain();
                Transmission = new Transmission();
                TractionControl = new TractionControl();
                ProfileSwitcher = new ProfileSwitcher();
                Speedlimiter = new Speedlimiter();
                DrivetrainCalibrator = new DrivetrainCalibrator();

                // Controls
                Controls = new ControlChain();

                Data.Run();
            }
        }

        public static void Store(IEnumerable<IniValueObject> settings, string f)
        {
            StringBuilder export = new StringBuilder();
            // Groups
            var groups = settings.Select(x => x.Group).Distinct();

            foreach (var group in groups)
            {
                export.AppendLine("[" + group + "]");

                foreach (var setting in settings.Where(x => x.Group == group))
                {
                    export.AppendLine(setting.Key + "=" + setting.RawValue);
                }

                export.AppendLine(" ");
            }
            try
            {
                File.WriteAllText(f, export.ToString());
                Debug.WriteLine("Exported settings to " + f);
            }catch
            {
            }
        }

        public static bool Load(IConfigurable target, string iniFile)
        {
            // Reset to default
            target.ResetParameters();
            try
            {
                // Load custom settings from INI file
                using (var ini = new IniReader(iniFile, true))
                {
                    ini.AddHandler((x) =>
                                       {
                                           if (target.AcceptsConfigs.Any(y => y == x.Group))
                                           {
                                               target.ApplyParameter(x);
                                           }

                                       });
                    ini.Parse();
                }
                return true;
            }catch
            {
                Debug.WriteLine("Failed to load configuration from " + iniFile);
            }
            return false;
            // DONE :)
        }

        public static void Start()
        {
            if (requiresSetup)
                Setup();
            //
            if (!Running)
            {
                Data.DataReceived += tick;
                Running = true;
            }
        }

        public static void Stop()
        {
            if (Running)
            {
                Data.DataReceived -= tick;
                Running = false;
            }
        }

        public static void tick(object sender, EventArgs e)
        {
            Controls.Tick(Data.Active);
        }

        #region Control mapping
        public static bool GetButtonIn(JoyControls c)
        {
            switch(c)
            {
                    // Unimplemented as of now.
                case Services.JoyControls.Gear1:
                case Services.JoyControls.Gear2:
                case Services.JoyControls.Gear3:
                case Services.JoyControls.Gear4:
                case Services.JoyControls.Gear5:
                case Services.JoyControls.Gear6:
                case Services.JoyControls.Gear7:
                case Services.JoyControls.Gear8:
                case Services.JoyControls.GearR:
                    return false;

                    // PS3 (via DS3 tool) L1/R1
                case Services.JoyControls.GearDown:
                    return RawJoysticksIn[0].GetButton(4);
                case Services.JoyControls.GearUp:
                    return RawJoysticksIn[0].GetButton(5);
                case Services.JoyControls.CruiseControl:
                    return RawJoysticksIn[0].GetButton(0);

                default:
                    return false;
            }
            // Map user config -> controller
        }

        public static double GetAxisIn(JoyControls c)
        {
            switch(c)
            {
                case Services.JoyControls.Throttle:
                    var t = ((RawJoysticksIn[0].GetAxis(3)/Math.Pow(2, 16) - 0.5)*2-0.25)/0.75;
                    if (t < 0) t = 0;
                   //t = t*t;
                    //t *= 0.8;
                    return t;

                case Services.JoyControls.Brake:
                    return ((RawJoysticksIn[0].GetAxis(2) - Math.Pow(2, 15)) / Math.Pow(2, 15)- 0.25)/0.75;

                case Services.JoyControls.Clutch:
                    return 0.0;
                    
                default:
                    return 0.0;
            }
        }

        public static void SetButtonOut(JoyControls c, bool value)
        {
            switch (c)
            {
                default:
                    break;

                case Services.JoyControls.Gear1:
                    RawJoysticksOut[0].SetButton(1, value);
                    break;

                case Services.JoyControls.Gear2:
                    RawJoysticksOut[0].SetButton(2, value);
                    break;

                case Services.JoyControls.Gear3:
                    RawJoysticksOut[0].SetButton(3, value);
                    break;

                case Services.JoyControls.Gear4:
                    RawJoysticksOut[0].SetButton(4, value);
                    break;

                case Services.JoyControls.Gear5:
                    RawJoysticksOut[0].SetButton(5, value);
                    break;

                case Services.JoyControls.Gear6:
                    RawJoysticksOut[0].SetButton(6, value);
                    break;

                case Services.JoyControls.Gear7:
                    RawJoysticksOut[0].SetButton(11, value);
                    break;

                case Services.JoyControls.Gear8:
                    RawJoysticksOut[0].SetButton(12, value);
                    break;

                case Services.JoyControls.GearR:
                    RawJoysticksOut[0].SetButton(7, value);
                    break;

                case Services.JoyControls.GearRange1:
                    RawJoysticksOut[0].SetButton(8, value);
                    break;

                case Services.JoyControls.GearRange2:
                    RawJoysticksOut[0].SetButton(9, value);
                    break;

                case Services.JoyControls.CruiseControl:
                    RawJoysticksOut[0].SetButton(10, value);
                    break;
            }
            try
            {
                if (ButtonFeedback.ContainsKey(c)) ButtonFeedback[c] = value;
                else ButtonFeedback.Add(c, value);
            }catch
            {
            }
        }

        public static void SetAxisOut(JoyControls c, double value)
        {
            switch(c)
            {
                default:
                    break;

                case Services.JoyControls.Throttle:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_X, value);
                    break;

                case Services.JoyControls.Brake:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_Y, value);
                    break;

                case Services.JoyControls.Clutch:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_Z, value);
                    break;
            }

            try
            {
                if (AxisFeedback.ContainsKey(c)) AxisFeedback[c] = value;
                else AxisFeedback.Add(c, value);
            }catch{}
        }
        #endregion

        public static double GetAxisOut(JoyControls ctrl)
        {
            if (AxisFeedback.ContainsKey(ctrl)) return AxisFeedback[ctrl];
            return 0;
        }

        public static bool GetButtonOut(JoyControls ctrl)
        {
            if (ButtonFeedback.ContainsKey(ctrl)) return ButtonFeedback[ctrl];
            return false;
        }

        private static int profileIndexLoaded = 0;
        public static void LoadNextProfile()
        {
            if (profileIndexLoaded >= CarProfile.Loaded.Count) profileIndexLoaded = 0;
            if (CarProfile.Loaded.Count == 0) return;
            CarProfile.Load(CarProfile.Loaded.Skip(profileIndexLoaded).FirstOrDefault().Name);
            profileIndexLoaded++;
            if (profileIndexLoaded >= CarProfile.Loaded.Count)
            {
                profileIndexLoaded = 0;
            }
        }
    }
}
