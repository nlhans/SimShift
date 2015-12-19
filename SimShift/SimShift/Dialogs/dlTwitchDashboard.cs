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
    public partial class dlTwitchDashboard : Form
    {
        private ucDashboard dsh;

        public dlTwitchDashboard()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            var updateUi = new Timer();
            updateUi.Interval = 20;
            updateUi.Tick += new EventHandler(updateUi_Tick);
            updateUi.Start();
            dsh = new ucDashboard(Color.FromArgb(5, 5, 5));
            dsh.Dock = DockStyle.Fill;
            Controls.Add(dsh);

            this.StartPosition = FormStartPosition.CenterScreen;
        }

        void updateUi_Tick(object sender, EventArgs e)
        {
            dsh.Invalidate();

        }

    }
}
