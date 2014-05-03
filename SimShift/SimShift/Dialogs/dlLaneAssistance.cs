using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimShift.Dialogs
{
    public partial class dlLaneAssistance : Form
    {
        private Timer UpdateMirrors = new Timer();

        public dlLaneAssistance()
        {
            InitializeComponent();

            this.Size = new Size(Services.LaneAssistance.mirrorWidth*2 + 100, Services.LaneAssistance.mirrorHeight*2 + 100);

            pbIn.Location = new Point(0, 0);
            pbProc.Location = new Point(0, Services.LaneAssistance.mirrorHeight+50);

            pbIn.Size = new Size(Services.LaneAssistance.mirrorWidth*2, Services.LaneAssistance.mirrorHeight);
            pbProc.Size = new Size(Services.LaneAssistance.mirrorWidth * 2, Services.LaneAssistance.mirrorHeight);

            UpdateMirrors.Interval = 100;
            UpdateMirrors.Tick += new EventHandler(UpdateMirrors_Tick);
            UpdateMirrors.Start();
        }

        void UpdateMirrors_Tick(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(UpdateMirrors_Tick), new object[2] {sender, e});
                return;
            }

            if (Services.LaneAssistance.CameraInput != null)
                pbIn.BackgroundImage = Services.LaneAssistance.CameraInput;
            if (Services.LaneAssistance.CameraOutput != null)
                pbProc.BackgroundImage = Services.LaneAssistance.CameraOutput;

            pbIn.Invalidate();
            pbProc.Invalidate();

        }
    }
}
