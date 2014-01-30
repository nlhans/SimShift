using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace SimShift.Controllers
{
    public delegate void JoystickButtonPress(JoystickInput joystickDevice, int button, bool state);
    public delegate void JoystickButtonEvent(JoystickInput joystickDevice, int button);

    public class JoystickInput
    {
        private JOYINFOEX _joyInfo;
        private JoystickInputDevice dev;
        private Timer joystickUpdate;

        public JoystickButtonPress State;
        public JoystickButtonEvent Press;
        public JoystickButtonEvent Release;

        public Dictionary<int, string> AxisNames { get { return dev.AxisNames; } }

        private readonly List<bool> _buttonState = new List<bool>();
        private readonly List<double> _axisState = new List<double>();

        public JoystickInput(JoystickInputDevice dev)
        {
            this.dev = dev;

            joystickUpdate = new Timer();
            joystickUpdate.Interval = 10;
            joystickUpdate.Elapsed += JoystickUpdateTick;
            joystickUpdate.Start();

            for (int i = 0; i < 6; i++)
                _axisState.Add(0);

            _joyInfo.dwSize = Marshal.SizeOf(_joyInfo);
            _joyInfo.dwFlags = JoystickFlags.JOY_RETURNALL;
        }

        private void JoystickUpdateTick(object sender, EventArgs e)
        {
            JoystickMethods.joyGetPosEx(dev.id, out _joyInfo);

            //for (int i = 0; i < 6; i++)
            //    Debug.WriteLine(i + ": " + _axisState[i]);
            //Debug.WriteLine(" :) ");
                // Take all axis inputs.
            _axisState[0] = _joyInfo.dwXpos;
            _axisState[1] = _joyInfo.dwYpos;
            _axisState[2] = _joyInfo.dwZpos;
            _axisState[3] = _joyInfo.dwRpos;
            _axisState[4] = _joyInfo.dwUpos;
            _axisState[5] = _joyInfo.dwVpos;

            // Take all button inputs.
            for (int i = 0; i < 32; i++)
            {
                var bitmask = _joyInfo.dwButtons & ((int)Math.Pow(2, i));
                if (_buttonState.Count <= i)
                {
                    _buttonState.Add(bitmask != 0);
                }
                else
                {
                    if (bitmask != 0)
                    {
                        // Pressed
                        if (!_buttonState[i])
                        {
                            // EVENT press
                            if (State != null)
                                State(this, i, true);
                            if (Press != null)
                                Press(this, i);

                        }
                        _buttonState[i] = true;
                    }
                    else
                    {
                        if (_buttonState[i])
                        {
                            // EVENT release
                            if (State != null)
                                State(this, i, false);
                            if (Release != null)
                                Release(this, i);
                        }
                        _buttonState[i] = false;
                    }
                }
            }
        }

        public double GetAxis(int id)
        {
            return id < _axisState.Count ? _axisState[id] : 0;
        }

        public bool GetButton(int id)
        {
            return id < _buttonState.Count && _buttonState[id];
        }
    }
}
