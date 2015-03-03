using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Entities;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class ucJoystickChannel : UserControl
    {
        private JoyControls ctrl;
        public bool Input { get; private set; }

        private bool isJoystickInput;
        private int index;
        private bool isAxis;

        public ucJoystickChannel(JoyControls c, bool inout)
        {
            InitializeComponent();
            lblControl.Text = c.ToString();
            pbVal.Value = 0;
            Input = inout;
            ctrl = c;
        }

        public ucJoystickChannel(bool axis, int i)
        {
            InitializeComponent();
            isAxis = axis;
            index = i;
            isJoystickInput = true;

            lblControl.Text = ((axis) ? "Axis" : "Button") + " " + i;
            pbVal.Value = 0;
            Input = true;
        }

        public void Tick()
        {
            int output = 0;

            if (isJoystickInput)
            {
                if (isAxis)
                    output = (int)(100 * Main.Controller.GetAxis(index) / 0x7FFF);
                else if (index >= 20)
                    output = Main.Controller.GetPov(index - 20) ? 100 : 0;
                else
                    output = Main.Controller.GetButton(index) ? 100 : 0;
            }
            else
            {
                var axisValue = Input ? Main.GetAxisIn(ctrl) : Main.GetAxisOut(ctrl);
                var buttonValue = Input ? Main.GetButtonIn(ctrl) : Main.GetButtonOut(ctrl);

                output = (int)Math.Max(axisValue*100, buttonValue ? 100 : 0);
            }
            if (double.IsNaN(output)) output = 0;
            if (output > 99) output = 99;
            if (output < 0) output = 0;

            pbVal.Value = (int) output+1;
            pbVal.Value = (int) output;
        }
    }
}
