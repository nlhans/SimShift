using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimShift.Entities;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlJoysticks : Form
    {
        public List<ucJoystickChannel> controlsIn = new List<ucJoystickChannel>();
        public List<ucJoystickChannel> controlsOut = new List<ucJoystickChannel>();
        public List<ucJoystickChannel> joysticks = new List<ucJoystickChannel>();
        private Timer _mUpdateJoysticks;

        private Timer _mCalibrateButton;

        public dlJoysticks()
        {
            InitializeComponent();

            _mUpdateJoysticks = new Timer();
            _mUpdateJoysticks.Interval = 25;
            _mUpdateJoysticks.Tick += _mUpdateJoysticks_Tick;
            _mUpdateJoysticks.Start();

            Main.Setup();

            for (int c_ = 0; c_ < (int) (JoyControls.NUM_OF_CONTROLS); c_++)
            {
                JoyControls c = (JoyControls) c_;

                var ucIn = new ucJoystickChannel(c, true);
                var ucOut = new ucJoystickChannel(c, false);

                ucIn.Location = new Point(3, 23 + ucIn.Height*c_);
                ucOut.Location = new Point(3, 23 + ucOut.Height*c_);

                controlsIn.Add(ucIn);
                controlsOut.Add(ucOut);

                gbIn.Controls.Add(ucIn);
                gbOut.Controls.Add(ucOut);

                // add to combobox
                cbControl.Items.Add(((int) c).ToString() + ", " + c.ToString());
            }

            var a = 0;
            for (a = 0; a < 8;a++)
            {
                var uc = new ucJoystickChannel(true, a);
                uc.Location = new Point(3, 23 + uc.Height * a);

                joysticks.Add(uc);
                gbController.Controls.Add(uc);
            }

            for (int b = 0; b < 32; b++)
            {
                var uc = new ucJoystickChannel(false, b);
                uc.Location = new Point(3, 23 + uc.Height*(a + b));

                joysticks.Add(uc);
                gbController.Controls.Add(uc);
            }
        }

        private void _mUpdateJoysticks_Tick(object sender, EventArgs e)
        {
            foreach (var c in controlsIn) c.Tick();
            foreach (var c in controlsOut) c.Tick();
            foreach (var c in joysticks) c.Tick();
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
                    for (int i = 0; i < (int) JoyControls.NUM_OF_CONTROLS; i++)
                    {
                        Main.SetButtonOut((JoyControls) i, false);
                    }
                }
                int buttonState = 0;
                _mCalibrateButton = new Timer();
                _mCalibrateButton.Interval = 1500;
                _mCalibrateButton.Tick += (o, args) =>
                                              {
                                                  if (buttonState == 0)
                                                  {
                                                      buttonState = 1;
                                                      Main.SetAxisOut((JoyControls)buttonId, 1);
                                                      Main.SetButtonOut((JoyControls)buttonId, true);
                                                  }
                                                  else if (buttonState == 1)
                                                  {
                                                      buttonState = 2;
                                                      Main.SetAxisOut((JoyControls)buttonId, 0.5);
                                                      Main.SetButtonOut((JoyControls)buttonId, true);
                                                  }
                                                  else

                                                  {
                                                      buttonState = 0;
                                                      Main.SetAxisOut((JoyControls)buttonId, 0);
                                                      Main.SetButtonOut((JoyControls)buttonId, false);
                                                  }
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
