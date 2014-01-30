using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace SimShift.Controllers
{
    public class JoystickInputDevice
    {
        private const string RegKeyAxisData = @"SYSTEM\ControlSet001\Control\MediaProperties\PrivateProperties\Joystick\OEM";
        private const string RegKeyPlace = @"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\";
        private const string RegReferencePlace = @"System\CurrentControlSet\Control\MediaResources\Joystick\DINPUT.DLL\CurrentJoystickSettings";

        public int id;
        public JOYCAPS data;

        public Dictionary<int, string> AxisNames = new Dictionary<int, string>();

        public string Name { get; private set; }

        public JoystickInputDevice(JOYCAPS captured, int device)
        {
            id = device;

            // Copy all members.
            data = new JOYCAPS();
            data.szPname = captured.szPname;
            data.wMid = captured.wMid;
            data.wPid = captured.wPid;
            data.wXmin = captured.wXmin;
            data.wXmax = captured.wXmax;
            data.wYmin = captured.wYmin;
            data.wYmax = captured.wYmax;
            data.wZmin = captured.wZmin;
            data.wZmax = captured.wZmax;
            data.wNumButtons = captured.wNumButtons;
            data.wPeriodMin = captured.wPeriodMin;
            data.wPeriodMax = captured.wPeriodMax;

            // Search register.
            RegistryKey rf = Registry.CurrentUser.OpenSubKey(RegReferencePlace);
            string USBDevice = Convert.ToString(rf.GetValue("Joystick" + (1 + id).ToString() + "OEMName"));
            RegistryKey usb = Registry.CurrentUser.OpenSubKey(RegKeyPlace);
            usb = usb.OpenSubKey(USBDevice);
            Name = (string)usb.GetValue("OEMName");

            // Get axis names
            RegistryKey axisMaster = Registry.LocalMachine.OpenSubKey(RegKeyAxisData).OpenSubKey(USBDevice);

            AxisNames = new Dictionary<int, string>();
            if (axisMaster != null)
            {
                axisMaster = axisMaster.OpenSubKey("Axes");
                if (axisMaster != null)
                {
                    foreach (string name in axisMaster.GetSubKeyNames())
                    {
                        RegistryKey axis = axisMaster.OpenSubKey(name);
                        AxisNames.Add(Convert.ToInt32(name), (string)axis.GetValue(""));
                        axis.Close();
                    }
                    axisMaster.Close();
                }
            }
            rf.Close();
            usb.Close();
        }

        public static IEnumerable<JoystickInputDevice> Search(string name)
        {
            var results1 = Search();
            var results2 = results1.Where(dev => dev.Name.ToLower().Contains(name.ToLower()));

            return results2;
        }


        /******************* STATIC ******************/
        static int deviceNumber = 0;
        public static IEnumerable<JoystickInputDevice> Search()
        {
            List<JoystickInputDevice> Joysticks = new List<JoystickInputDevice>();

            JOYCAPS CapturedJoysticks;
            uint devs = JoystickMethods.joyGetNumDevs();
            for (deviceNumber = 0; deviceNumber < devs; deviceNumber++)
            {
                UInt32 res = JoystickMethods.joyGetDevCaps(deviceNumber, out CapturedJoysticks, JOYCAPS.Size);
                if (res != 165)
                {
                    Joysticks.Add(new JoystickInputDevice(CapturedJoysticks, deviceNumber));
                }
            }

            return Joysticks;
        }

    }
}