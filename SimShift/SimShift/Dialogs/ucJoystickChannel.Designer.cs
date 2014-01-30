namespace SimShift.Dialogs
{
    partial class ucJoystickChannel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblControl = new System.Windows.Forms.Label();
            this.pbVal = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // lblControl
            // 
            this.lblControl.AutoSize = true;
            this.lblControl.Location = new System.Drawing.Point(3, 3);
            this.lblControl.Name = "lblControl";
            this.lblControl.Size = new System.Drawing.Size(35, 13);
            this.lblControl.TabIndex = 0;
            this.lblControl.Text = "label1";
            // 
            // pbVal
            // 
            this.pbVal.Location = new System.Drawing.Point(75, 0);
            this.pbVal.Name = "pbVal";
            this.pbVal.Size = new System.Drawing.Size(80, 20);
            this.pbVal.TabIndex = 1;
            // 
            // ucJoystickChannel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pbVal);
            this.Controls.Add(this.lblControl);
            this.Name = "ucJoystickChannel";
            this.Size = new System.Drawing.Size(155, 20);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblControl;
        private System.Windows.Forms.ProgressBar pbVal;
    }
}
