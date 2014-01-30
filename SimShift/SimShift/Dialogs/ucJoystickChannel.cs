using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class ucJoystickChannel : UserControl
    {
        private JoyControls ctrl;
        public bool Input { get; private set; }

        public ucJoystickChannel(JoyControls c, bool inout)
        {
            InitializeComponent();
            lblControl.Text = c.ToString();
            pbVal.Value = 0;
            Input = inout;
            ctrl = c;
        }

        public void Tick()
        {
            var axisValue = Input ? Main.GetAxisIn(ctrl) : Main.GetAxisOut(ctrl);
            var buttonValue =Input ? Main.GetButtonIn(ctrl) : Main.GetButtonOut(ctrl);

            var output = Math.Max(axisValue*100, buttonValue ? 100 : 0);
            if (output > 99) output = 99;
            if (output < 0) output = 0;


            pbVal.Value = (int) output+1;
            pbVal.Value = (int) output;
        }
    }
}
