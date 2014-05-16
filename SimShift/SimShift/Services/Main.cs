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
        public static PowerLimiter PowerLimiter;
        public static LaunchControl LaunchControl;
        public static LaneAssistance LaneAssistance;
        
        public static ProfileSwitcher ProfileSwitcher;
        public static CameraHorizon CameraHorizon;
       

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
                JoystickInput ps3Cont, g25Cont;
                if (ps3 == null)
                    ps3Cont = default(JoystickInput);
                else
                    ps3Cont = new JoystickInput(ps3);
                var g25 = JoystickInputDevice.Search("G25").FirstOrDefault();
                if (g25 == null)
                    g25Cont = default(JoystickInput);
                else
                    g25Cont = new JoystickInput(g25);
                var vJoy = new JoystickOutput();

                RawJoysticksIn.Add(ps3Cont);
                RawJoysticksIn.Add(g25Cont);
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

                // TODO: Temporary..
                Data.AppActive += (s, e) =>
                                      {
                                          if (Data.Active.Application == "TestDrive2")
                                          {
                                              CameraHorizon.CameraHackEnabled = true;
                                          }
                                          else
                                          {
                                              CameraHorizon.CameraHackEnabled = false;
                                          }
                                      };

                // Modules
                Antistall = new Antistall();
                CruiseControl = new CruiseControl();
                Drivetrain = new GenericDrivetrain();
                Transmission = new Transmission();
                TractionControl = new TractionControl();
                ProfileSwitcher = new ProfileSwitcher();
                Speedlimiter = new Speedlimiter();
                PowerLimiter = new PowerLimiter();
                LaunchControl = new LaunchControl();
                DrivetrainCalibrator = new DrivetrainCalibrator();
                LaneAssistance = new LaneAssistance();

                CameraHorizon = new CameraHorizon();

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
            Debug.WriteLine("Loading configuration file " + iniFile);
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

        private static bool ps3Controller = false;

        public static bool GetButtonIn(JoyControls c)
        {
            switch (c)
            {
                    // Unimplemented as of now.
                case Services.JoyControls.Gear1:
                    return RawJoysticksIn[1].GetButton(8);
                case Services.JoyControls.Gear2:
                    return RawJoysticksIn[1].GetButton(9);
                case Services.JoyControls.Gear3:
                    return RawJoysticksIn[1].GetButton(10);
                case Services.JoyControls.Gear4:
                    return RawJoysticksIn[1].GetButton(11);
                case Services.JoyControls.Gear5:
                    return RawJoysticksIn[1].GetButton(12);
                case Services.JoyControls.Gear6:
                    return RawJoysticksIn[1].GetButton(13);
                case Services.JoyControls.Gear7:
                    return false;
                case Services.JoyControls.Gear8:
                    return false;
                case Services.JoyControls.GearR:
                    return RawJoysticksIn[1].GetButton(14);

                case JoyControls.GearRange1:
                    return RawJoysticksIn[1].GetButton(6);

                case Services.JoyControls.LaneAssistance:
                    if (ps3Controller)
                        return false;
                    else
                        return false; //return RawJoysticksIn[1].GetButton(7);

                    // PS3 (via DS3 tool) L1/R1
                case Services.JoyControls.GearDown:
                    if (ps3Controller)
                        return RawJoysticksIn[0].GetButton(4);
                    else if (Transmission.Enabled)
                        return RawJoysticksIn[1].GetButton(8);
                    else return false;
                case Services.JoyControls.GearUp:
                    if (ps3Controller)
                        return RawJoysticksIn[0].GetButton(5);
                    else if (Transmission.Enabled)
                        return RawJoysticksIn[1].GetButton(9);
                    else return false;
                case Services.JoyControls.CruiseControl:
                    if (ps3Controller)
                        return RawJoysticksIn[0].GetButton(0);
                    else
                        return RawJoysticksIn[1].GetButton(15);
                case Services.JoyControls.LaunchControl:
                    if (ps3Controller)
                        return RawJoysticksIn[0].GetButton(11);
                    else
                        return false && RawJoysticksIn[1].GetButton(18);

                default:
                    return false;
            }
            // Map user config -> controller
        }

        public static double GetAxisIn(JoyControls c)
        {
            switch(c)
            {
                case Services.JoyControls.Steering:

                    if (ps3Controller)
                        return 0.5;
                    else
                        return RawJoysticksIn[1].GetAxis(0)/Math.Pow(2,16);
                    
                case Services.JoyControls.Throttle:

                    double t = 0;
                    if (ps3Controller)
                        t = ((RawJoysticksIn[0].GetAxis(3) / Math.Pow(2, 16) - 0.5) * 2 - 0.25) / 0.75;
                    else
                        t = 1 - RawJoysticksIn[1].GetAxis(2)/Math.Pow(2, 16);
                    if (t < 0) t = 0;
                   
                if (Main.Data.Active!=null && Main.Data.Active.Application.Contains("ruck")) t = t*t;
                    //t *= 0.8;
                    return t;

                case Services.JoyControls.Brake:
                    if (ps3Controller)
                        return ((RawJoysticksIn[0].GetAxis(2) - Math.Pow(2, 15)) / Math.Pow(2, 15) - 0.25) / 0.75;
                    else
                    {
                        var b = 1 - RawJoysticksIn[1].GetAxis(3)/Math.Pow(2, 16);
                        if (b < 0) b = 0;
                        
                        if (Main.Data.Active == null || Main.Data.Active.Application == "TestDrive2") return b;
                        return b * b;
                    }
                case Services.JoyControls.Clutch:
                    return 1 - RawJoysticksIn[1].GetAxis(4) / Math.Pow(2, 16);

                case Services.JoyControls.CameraHorizon:
                    if (ps3Controller)
                        return RawJoysticksIn[0].GetAxis(5) / Math.Pow(2, 15) - 1;
                    else return 0;
                    
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

                case Services.JoyControls.Steering:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_RX, value/2);
                    break;

                case Services.JoyControls.Throttle:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_X, value/2);
                    break;

                case Services.JoyControls.Brake:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_Y, value/2);
                    break;

                case Services.JoyControls.Clutch:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_Z, value/2);
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
