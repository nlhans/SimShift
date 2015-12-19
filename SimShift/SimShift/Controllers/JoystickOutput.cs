using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using vJoyInterfaceWrap;

namespace SimShift.Controllers
{

    public class JoystickOutput
    {
        public uint ID;
        public vJoy joy;

        public JoystickOutput()
        {
            int id = 1;
            ID = (uint)id;
            joy = initVjoy(ID);
        }
        private vJoy initVjoy(uint id)
        {
            var joystick = new vJoy();
            var iReport = new vJoy.JoystickState();

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            /*switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    MessageBox.Show(string.Format("vJoy Device {0} is already owned by this feeder\n", id));
                    break;
                case VjdStat.VJD_STAT_FREE:
                    MessageBox.Show(string.Format("vJoy Device {0} is free\n", id));
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    MessageBox.Show(string.Format("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id));
                    return joystick;
                case VjdStat.VJD_STAT_MISS:
                    MessageBox.Show(string.Format("vJoy Device {0} is not installed or disabled\nCannot continue\n", id));
                    return joystick;
                default:
                    MessageBox.Show(string.Format("vJoy Device {0} general error\nCannot continue\n", id));
                    return joystick;
            }*/
            bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            if (joystick.AcquireVJD(id) == false)
                MessageBox.Show("Could not acquire vJoy " + id);
            Console.WriteLine(AxisX);
            return joystick;
        }

        public void SetButton(int btnId, bool v)
        {
            joy.SetBtn(v, ID, (uint)btnId);
        }
        public void SetAxis(HID_USAGES axisId, double v)
        {
            joy.SetAxis((int)(v * Math.Pow(2, 16)), ID, axisId);
        }

    }

    /*
    public class JoystickOutput
    {
        public const string PPJOY_1 = @"\\.\PPJoyIOCTL1";

        #region Win32API stuff
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, EFileAccess dwDesiredAccess, EFileShare dwShareMode, IntPtr lpSecurityAttributes, ECreationDisposition dwCreationDisposition, EFileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref JOYSTICK_STATE lpInBuffer, Int32 nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        private enum EFileAccess : uint
        {
            GenericWrite = 0x40000000,
            GenericAll = 0x10000000
        }
        private enum EFileShare : uint { Write = 0x00000002 }
        private enum ECreationDisposition : uint { OpenExisting = 3 }
        private enum EFileAttributes : uint { None = 0x00000000 }
        private enum EFileDevice : uint { Unknown = 0x00000022 }
        private enum EMethod : uint { Buffered = 0 }

        private const string DeviceName = @"\\.\PPJoyIOCTL1";
        private const uint JOYSTICK_STATE_V1 = 0x53544143;
        private const int JOYSTICK_STATE_SIZE = 54;
        private const int PPJOY_AXIS_MIN = 1;
        private const int PPJOY_AXIS_MAX = 32767;

        [StructLayout(LayoutKind.Explicit, Size = JOYSTICK_STATE_SIZE)]
        protected struct JOYSTICK_STATE
        {
            [FieldOffset(0)]
            public uint Signature;
            [FieldOffset(4)]
            public byte NumAnalog;
            [FieldOffset(5)]
            public int Analog1;
            [FieldOffset(9)]
            public int Analog2;
            [FieldOffset(13)]
            public int Analog3;
            [FieldOffset(17)]
            public int Analog4;
            [FieldOffset(21)]
            public byte NumDigital;
            [FieldOffset(22)]
            public byte Digital1;
            [FieldOffset(23)]
            public byte Digital2;
            [FieldOffset(24)]
            public byte Digital3;
            [FieldOffset(25)]
            public byte Digital4;
            [FieldOffset(26)]
            public byte Digital5;
            [FieldOffset(27)]
            public byte Digital6;
            [FieldOffset(28)]
            public byte Digital7;
            [FieldOffset(29)]
            public byte Digital8;
            [FieldOffset(30)]
            public byte Digital9;
            [FieldOffset(31)]
            public byte Digital10;
            [FieldOffset(32)]
            public byte Digital11;
            [FieldOffset(33)]
            public byte Digital12;
            [FieldOffset(34)]
            public byte Digital13;
            [FieldOffset(35)]
            public byte Digital14;
            [FieldOffset(36)]
            public byte Digital15;
            [FieldOffset(37)]
            public byte Digital16;
            [FieldOffset(38)]
            public byte Digital17;
            [FieldOffset(39)]
            public byte Digital18;
            [FieldOffset(40)]
            public byte Digital19;
            [FieldOffset(41)]
            public byte Digital20;
            [FieldOffset(42)]
            public byte Digital21;
            [FieldOffset(43)]
            public byte Digital22;
            [FieldOffset(44)]
            public byte Digital23;
            [FieldOffset(45)]
            public byte Digital24;
            [FieldOffset(46)]
            public byte Digital25;
            [FieldOffset(47)]
            public byte Digital26;
            [FieldOffset(48)]
            public byte Digital27;
            [FieldOffset(49)]
            public byte Digital28;
            [FieldOffset(50)]
            public byte Digital29;
            [FieldOffset(51)]
            public byte Digital30;
            [FieldOffset(52)]
            public byte Digital31;
            [FieldOffset(53)]
            public byte Digital32;
        }

        private static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
        }
        private static uint IOCTL_PPORTJOY_SET_STATE = CTL_CODE((uint)EFileDevice.Unknown, 0, (uint)EMethod.Buffered, (uint)EFileAccess.GenericAll);

        private static IntPtr Dev;
        private static JOYSTICK_STATE joyState = new JOYSTICK_STATE();

        #endregion

        public static ManualResetEvent NewPackage = new ManualResetEvent(false);
        private Thread _th;
        private List<double> _dataA = new List<double>();
        private List<bool> _dataB = new List<bool>();
        private bool _running = false;

        private System.Timers.Timer _updateTimer;

        public void OverwriteData(List<double> axis, List<bool> buttons)
        {
            _dataA = axis;

            for (int k = 0; k < _dataA.Count; k++)
            {
                if (double.IsNaN(_dataA[k]) || double.IsInfinity(_dataA[k]))
                    _dataA[k] = 0;
                if (_dataA[k] > 1) _dataA[k] = 1;
                if (_dataA[k] < 0)
                    _dataA[k] = 0;
            }

            _dataB = buttons;

        }

        public JoystickOutput()
        {
            _running = true;

            _dataA = new List<double>();
            while (_dataA.Count < 4) _dataA.Add(0);
            while(_dataB.Count < 32) _dataB.Add(false);


                // Initalize the controller
                Dev = CreateFile(DeviceName, EFileAccess.GenericWrite, EFileShare.Write, IntPtr.Zero, ECreationDisposition.OpenExisting, EFileAttributes.None, IntPtr.Zero);

            if (Dev.ToInt32() == -1)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            joyState = new JOYSTICK_STATE();

            joyState.Signature = JOYSTICK_STATE_V1;

            joyState.NumAnalog = 4;
            joyState.NumDigital = 32;

            joyState.Analog1 = 1;
            joyState.Analog2 = 1;
            joyState.Analog3 = 1;
            joyState.Analog4 = 1;
            joyState.Digital1 = 0;
            joyState.Digital2 = 0;
            joyState.Digital3 = 0;
            joyState.Digital4 = 0;
            joyState.Digital5 = 0;
            joyState.Digital6 = 0;
            joyState.Digital7 = 0;
            joyState.Digital8 = 0;
            joyState.Digital9 = 0;
            joyState.Digital10 = 0;
            joyState.Digital11 = 0;
            joyState.Digital12 = 0;
            joyState.Digital13 = 0;
            joyState.Digital14 = 0;
            joyState.Digital15 = 0;
            joyState.Digital16 = 0;
            joyState.Digital17 = 0;
            joyState.Digital18 = 0;
            joyState.Digital19 = 0;
            joyState.Digital20 = 0;
            joyState.Digital21 = 0;
            joyState.Digital22 = 0;
            joyState.Digital23 = 0;
            joyState.Digital24 = 0;
            joyState.Digital25 = 0;
            joyState.Digital26 = 0;
            joyState.Digital27 = 0;
            joyState.Digital28 = 0;
            joyState.Digital29 = 0;
            joyState.Digital30 = 0;
            joyState.Digital31 = 0;
            joyState.Digital32 = 0;

            _th = new Thread(_DeviceWriter);
            _th.Start();

            _updateTimer = new System.Timers.Timer {Interval = 10};
            _updateTimer.Elapsed += (s, e) => NewPackage.Set();
            _updateTimer.Start();
        }

        public void Close()
        {
            _updateTimer.Close();
            _running = false;
            NewPackage.Set();
        }

        private void _DeviceWriter()
        {
            int _min = PPJOY_AXIS_MIN;
            int _max = PPJOY_AXIS_MAX - _min;

            while (_running)
            {
                NewPackage.WaitOne();
                if (!_running) break;
                int i = 0;
                joyState.Digital1 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital2 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital3 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital4 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital5 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital6 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital7 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital8 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital9 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital10 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital11 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital12 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital13 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital14 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital15 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital16 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital17 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital18 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital19 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital20 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital21 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital22 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital23 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital24 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital25 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital26 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital27 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital28 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital29 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital30 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital31 = (byte)((this._dataB[i]) ? 1 : 0); i++;
                joyState.Digital32 = (byte)((this._dataB[i]) ? 1 : 0); i++;

                joyState.Analog1 = Convert.ToInt32(Math.Round(_min + _max * _dataA[0]));
                joyState.Analog2 = Convert.ToInt32(Math.Round(_min + _max * _dataA[1]));
                joyState.Analog3 = Convert.ToInt32(Math.Round(_min + _max * _dataA[2]));
                joyState.Analog4 = Convert.ToInt32(Math.Round(_min + _max * _dataA[3]));

                try
                {
                    uint result;
                    if (!DeviceIoControl(Dev, IOCTL_PPORTJOY_SET_STATE, ref joyState, JOYSTICK_STATE_SIZE, IntPtr.Zero, 0, out result, IntPtr.Zero))
                    {
                        int error = Marshal.GetHRForLastWin32Error();
                        if (error == 2)
                            throw new InvalidOperationException("Underlying joystick device deleted.");
                        else
                            Marshal.ThrowExceptionForHR(error);
                    }
                }
                catch (Exception e)
                {
                    break;
                }
                NewPackage.Reset();

            }
            CloseHandle(Dev);
        }

        public void SetButton(int b, bool v)
        {
            if (b >= _dataB.Count) return;
            _dataB[b] = v;
        }

        public void SetAxis(int b, double v)
        {
            if (b >= _dataA.Count) return;
            _dataA[b] = v;
        }
    }
    */
}