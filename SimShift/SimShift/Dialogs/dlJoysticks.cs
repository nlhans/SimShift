using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlJoysticks : Form
    {
        public List<ucJoystickChannel> controlsIn = new List<ucJoystickChannel>();
        public List<ucJoystickChannel> controlsOut = new List<ucJoystickChannel>();
        private Timer _mUpdateJoysticks;

        private Timer _mCalibrateButton;

        public dlJoysticks()
        {
            InitializeComponent();

            _mUpdateJoysticks = new Timer();
            _mUpdateJoysticks.Interval = 50;
            _mUpdateJoysticks.Tick += new EventHandler(_mUpdateJoysticks_Tick);
            _mUpdateJoysticks.Start();

            Main.Setup();

            for (int c_ = 0; c_ < (int) (JoyControls.NUM_OF_CONTROLS); c_++)
            {
                JoyControls c = (JoyControls) c_;

                var ucIn = new ucJoystickChannel(c, true);
                var ucOut = new ucJoystickChannel(c, false);

                ucIn.Location = new Point(10, 50 + ucIn.Height*c_);
                ucOut.Location = new Point(210, 50 + ucOut.Height*c_);

                controlsIn.Add(ucIn);
                controlsOut.Add(ucOut);

                Controls.Add(ucIn);
                Controls.Add(ucOut);

                // add to combobox
                cbControl.Items.Add(((int) c).ToString() + ", " + c.ToString());
            }
        }

        private void _mUpdateJoysticks_Tick(object sender, EventArgs e)
        {
            foreach (var c in controlsIn) c.Tick();
            foreach (var c in controlsOut) c.Tick();
        }
        int buttonId = 0;
        private void btDoCal_Click(object sender, EventArgs e)
        {
            if (_mCalibrateButton == null)
            {
                try
                {
                    buttonId = int.Parse(cbControl.SelectedItem.ToString().Split(",".ToCharArray()).FirstOrDefault());
                }
                catch(Exception)
                {
                    MessageBox.Show("Cannot parse button");
                }
                if (Main.Running)
                {
                    MessageBox.Show("This will stop main service");
                    Main.Stop();
                }
                bool buttonState = false;
                _mCalibrateButton = new Timer();
                _mCalibrateButton.Interval = 500;
                _mCalibrateButton.Tick += (o, args) =>
                                              {
                                                  if (buttonState)
                                                  {
                                                      Main.SetAxisOut((JoyControls)buttonId, 1);
                                                      Main.SetButtonOut((JoyControls)buttonId, true);
                                                  }
                                                  else
                                                  {
                                                      Main.SetAxisOut((JoyControls)buttonId, 0);
                                                      Main.SetButtonOut((JoyControls)buttonId, false);
                                                  }
                                                  buttonState = !buttonState;
                                              };
                _mCalibrateButton.Start();

                btDoCal.Text = "Stop calibration";
            }
            else
            {
                _mCalibrateButton.Stop();
                _mCalibrateButton = null;

                Main.SetAxisOut((JoyControls)buttonId, 0);
                Main.SetButtonOut((JoyControls)buttonId, false);

                btDoCal.Text = "Toggle for calibration";
            }
        }
    }
}
