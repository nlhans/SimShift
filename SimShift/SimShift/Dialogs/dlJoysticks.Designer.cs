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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbControl = new System.Windows.Forms.ComboBox();
            this.btDoCal = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Inputs:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(396, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Outputs:";
            // 
            // cbControl
            // 
            this.cbControl.FormattingEnabled = true;
            this.cbControl.Location = new System.Drawing.Point(12, 457);
            this.cbControl.Name = "cbControl";
            this.cbControl.Size = new System.Drawing.Size(166, 21);
            this.cbControl.TabIndex = 2;
            // 
            // btDoCal
            // 
            this.btDoCal.Location = new System.Drawing.Point(184, 457);
            this.btDoCal.Name = "btDoCal";
            this.btDoCal.Size = new System.Drawing.Size(142, 23);
            this.btDoCal.TabIndex = 3;
            this.btDoCal.Text = "Toggle for calibration";
            this.btDoCal.UseVisualStyleBackColor = true;
            this.btDoCal.Click += new System.EventHandler(this.btDoCal_Click);
            // 
            // dlJoysticks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(791, 490);
            this.Controls.Add(this.btDoCal);
            this.Controls.Add(this.cbControl);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "dlJoysticks";
            this.Text = "dlJoysticks";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbControl;
        private System.Windows.Forms.Button btDoCal;
    }
}