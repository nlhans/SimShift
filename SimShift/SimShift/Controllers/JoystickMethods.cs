using System;
using System.Runtime.InteropServices;

namespace SimShift.Controllers
{
    public static class JoystickMethods
    {
        [DllImport("Winmm.dll")]
        public static extern UInt32 joyGetDevCaps(Int32 uJoyID, out JOYCAPS pjc, Int32 cbjc);
        [DllImport("Winmm.dll")]
        public static extern UInt32 joyGetPosEx(Int32 uJoyID, out JOYINFOEX pji);
        [DllImport("Winmm.dll")]
        public static extern UInt32 joyGetNumDevs();
    }
    public enum JoystickError
    {
        NoError = 0,
        InvalidParameters = 165,
        NoCanDo = 166,
        Unplugged = 167
    }

    [Flags()]
    public enum JoystickFlags
    {
        JOY_RETURNX = 0x1,
        JOY_RETURNY = 0x2,
        JOY_RETURNZ = 0x4,
        JOY_RETURNR = 0x8,
        JOY_RETURNU = 0x10,
        JOY_RETURNV = 0x20,
        JOY_RETURNPOV = 0x40,
        JOY_RETURNBUTTONS = 0x80,
        JOY_RETURNALL = (JOY_RETURNX | JOY_RETURNY | JOY_RETURNZ | JOY_RETURNR | JOY_RETURNU | JOY_RETURNV | JOY_RETURNPOV | JOY_RETURNBUTTONS)
    }

    public class WinMM
    {
        // Verreweg van compleet
        public const int MAXPNAMELEN = 32;
    }
    [Flags]
    public enum JoystCapsFlags
    {
        HasZ = 0x1,
        HasR = 0x2,
        HasU = 0x4,
        HasV = 0x8,
        HasPov = 0x16,
        HasPov4Dir = 0x32,
        HasPovContinuous = 0x64
    }

    public struct JOYCAPS
    {
        public short wMid;
        public short wPid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WinMM.MAXPNAMELEN)]
        public string szPname;
        public uint wXmin;
        public uint wXmax;
        public uint wYmin;
        public uint wYmax;
        public uint wZmin;
        public uint wZmax;
        public uint wNumButtons;
        public uint wPeriodMin;
        public uint wPeriodMax;
        public uint RMin;
        public uint RMax;
        public uint UMin;
        public uint UMax;
        public uint VMin;
        public uint VMax;
        public JoystCapsFlags Capabilities;
        public uint MaxAxes;
        public uint NumAxes;
        public uint MaxButtons;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string RegKey;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string OemVxD;

        public static readonly int SizeInBytes;

        public static int Size { get { return Marshal.SizeOf(default(JOYCAPS)); } }
    }
    public struct JOYINFOEX
    {
        public int dwSize;
        public JoystickFlags dwFlags;
        public int dwXpos;
        public int dwYpos;
        public int dwZpos;
        public int dwRpos;
        public int dwUpos;
        public int dwVpos;
        public int dwButtons;
        public int dwButtonNumber;
        public int dwPOV;
        public int dwReserved1;
        public int dwReserved2;
    }
    public struct JOYINFO
    {
        public int wXpos;
        public int wYpos;
        public int wZpos;
        public int wButtons;
    }
}