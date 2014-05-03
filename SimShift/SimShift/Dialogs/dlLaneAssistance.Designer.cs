namespace SimShift.Dialogs
{
    partial class dlLaneAssistance
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
            this.pbIn = new System.Windows.Forms.PictureBox();
            this.pbProc = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbIn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbProc)).BeginInit();
            this.SuspendLayout();
            // 
            // pbIn
            // 
            this.pbIn.Location = new System.Drawing.Point(12, 12);
            this.pbIn.Name = "pbIn";
            this.pbIn.Size = new System.Drawing.Size(100, 50);
            this.pbIn.TabIndex = 0;
            this.pbIn.TabStop = false;
            // 
            // pbProc
            // 
            this.pbProc.Location = new System.Drawing.Point(12, 68);
            this.pbProc.Name = "pbProc";
            this.pbProc.Size = new System.Drawing.Size(100, 50);
            this.pbProc.TabIndex = 1;
            this.pbProc.TabStop = false;
            // 
            // dlLaneAssistance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(627, 543);
            this.Controls.Add(this.pbProc);
            this.Controls.Add(this.pbIn);
            this.Name = "dlLaneAssistance";
            this.Text = "dlLaneAssistance";
            ((System.ComponentModel.ISupportInitialize)(this.pbIn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbProc)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbIn;
        private System.Windows.Forms.PictureBox pbProc;
    }
}