using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
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

        public static JoystickInput Controller
        {
            get { return RawJoysticksIn[0]; }
        }


        public static DrivetrainCalibrator DrivetrainCalibrator;

        public static void Save()
        {
            if(Map!=null)
            Map.Export();
        }

        public static bool Setup()
        {
            if (requiresSetup)
            {
                requiresSetup = false;

                var hotas = JoystickInputDevice.Search("Hotas").FirstOrDefault();
                var hotasController = hotas == null ? default(JoystickInput) : new JoystickInput(hotas);
                var vJoy = new JoystickOutput();

                RawJoysticksIn.Add(hotasController);
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

                if (hotasController == null)
                {
                    MessageBox.Show("No controllers found");
                    return false;
                }
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
                return true;

            }
            return false;
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
            var isNowRunning = Running;
            if (requiresSetup)
                isNowRunning  = Setup();
            //
            if (!Running)
            {
                Data.DataReceived += tick;
                Running = isNowRunning;
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
            switch (c)
            {
                    // Unimplemented as of now.
                case Services.JoyControls.Gear1:
                    return Controller.GetButton(1);
                case Services.JoyControls.Gear2:
                    return false;
                case Services.JoyControls.Gear3:
                    return false;
                case Services.JoyControls.Gear4:
                    return false;
                case Services.JoyControls.Gear5:
                    return false;
                case Services.JoyControls.Gear6:
                    return false;
                case Services.JoyControls.Gear7:
                    return false;
                case Services.JoyControls.Gear8:
                    return false;
                case Services.JoyControls.GearR:
                    return Controller.GetButton(3);

                case JoyControls.GearRange1:
                    return false;

                case JoyControls.GearRange2:
                    return false;

                case Services.JoyControls.LaneAssistance:
                    return false;

                    // PS3 (via DS3 tool) L1/R1
                case Services.JoyControls.GearDown:
                    return Controller.GetButton(4);

                case Services.JoyControls.GearUp:
                    return Controller.GetButton(8);

                case Services.JoyControls.CruiseControlMaintain:
                    return Controller.GetButton(0);

                case JoyControls.CruiseControlUp:
                    return Controller.GetPov(2);
                case JoyControls.CruiseControlDown:
                    return Controller.GetPov(0);
                case JoyControls.CruiseControlOnOff:
                    return Controller.GetPov(1);


                case Services.JoyControls.LaunchControl:
                    return Controller.GetButton(11);

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
                    var steer1 = Controller.GetAxis(3) / Math.Pow(2, 15) - 1;
                    var steer2 = Controller.GetAxis(0)/Math.Pow(2,15) - 1;
                    if (steer1 < 0) steer1 = steer1*steer1*-1;
                    else steer1 *= steer1;
                    if (steer2 < 0) steer2 = steer2*steer2*-1;
                    else steer2 *= steer2;
                    if (Math.Abs(steer1) > Math.Abs(steer2)) return (steer1 + 1) / 2;
                    else return (steer2 + 1) / 2;

                case Services.JoyControls.Throttle:
                    var t = 0.5 - Controller.GetAxis(2) / Math.Pow(2, 16);
                    t *= 2;
                    if (t < 0) t = 0;
                    return t;

                case Services.JoyControls.Brake:
                    var b = Controller.GetAxis(2) / Math.Pow(2, 16) - 0.5;
                    b *= 2;
                    if (b < 0) b = 0;
                        
                    if (Main.Data.Active == null || Main.Data.Active.Application == "TestDrive2") return b;
                    return b * b;

                case Services.JoyControls.Clutch:
                    return 0;

                case Services.JoyControls.CameraHorizon:
                    return 0;
                    
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

                case Services.JoyControls.CruiseControlMaintain:
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
