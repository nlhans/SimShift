namespace SimShift.Dialogs
{
    partial class dlJoysticks
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cbControl = new System.Windows.Forms.ComboBox();
            this.btDoCal = new System.Windows.Forms.Button();
            this.gbIn = new System.Windows.Forms.GroupBox();
            this.gbOut = new System.Windows.Forms.GroupBox();
            this.gbController = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // cbControl
            // 
            this.cbControl.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbControl.FormattingEnabled = true;
            this.cbControl.Location = new System.Drawing.Point(12, 458);
            this.cbControl.Name = "cbControl";
            this.cbControl.Size = new System.Drawing.Size(210, 21);
            this.cbControl.TabIndex = 2;
            // 
            // btDoCal
            // 
            this.btDoCal.Location = new System.Drawing.Point(228, 458);
            this.btDoCal.Name = "btDoCal";
            this.btDoCal.Size = new System.Drawing.Size(142, 23);
            this.btDoCal.TabIndex = 3;
            this.btDoCal.Text = "Toggle for calibration";
            this.btDoCal.UseVisualStyleBackColor = true;
            this.btDoCal.Click += new System.EventHandler(this.btDoCal_Click);
            // 
            // gbIn
            // 
            this.gbIn.Location = new System.Drawing.Point(12, 12);
            this.gbIn.Name = "gbIn";
            this.gbIn.Size = new System.Drawing.Size(210, 440);
            this.gbIn.TabIndex = 4;
            this.gbIn.TabStop = false;
            this.gbIn.Text = "Joystick Inputs";
            // 
            // gbOut
            // 
            this.gbOut.Location = new System.Drawing.Point(228, 12);
            this.gbOut.Name = "gbOut";
            this.gbOut.Size = new System.Drawing.Size(210, 440);
            this.gbOut.TabIndex = 5;
            this.gbOut.TabStop = false;
            this.gbOut.Text = "Joystick Outputs";
            // 
            // gbController
            // 
            this.gbController.Location = new System.Drawing.Point(444, 12);
            this.gbController.Name = "gbController";
            this.gbController.Size = new System.Drawing.Size(249, 440);
            this.gbController.TabIndex = 6;
            this.gbController.TabStop = false;
            this.gbController.Text = "Joystick";
            // 
            // dlJoysticks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(708, 490);
            this.Controls.Add(this.gbController);
            this.Controls.Add(this.gbOut);
            this.Controls.Add(this.gbIn);
            this.Controls.Add(this.btDoCal);
            this.Controls.Add(this.cbControl);
            this.Name = "dlJoysticks";
            this.Text = "dlJoysticks";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cbControl;
        private System.Windows.Forms.Button btDoCal;
        private System.Windows.Forms.GroupBox gbIn;
        private System.Windows.Forms.GroupBox gbOut;
        private System.Windows.Forms.GroupBox gbController;
    }
}