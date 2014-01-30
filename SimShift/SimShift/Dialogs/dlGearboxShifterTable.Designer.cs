namespace SimShift.Dialogs
{
    partial class dlGearboxShifterTable
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
            this.lGearCount = new System.Windows.Forms.Label();
            this.shifterTable = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.shifterTable)).BeginInit();
            this.SuspendLayout();
            // 
            // lGearCount
            // 
            this.lGearCount.AutoSize = true;
            this.lGearCount.Location = new System.Drawing.Point(12, 9);
            this.lGearCount.Name = "lGearCount";
            this.lGearCount.Size = new System.Drawing.Size(90, 13);
            this.lGearCount.TabIndex = 0;
            this.lGearCount.Text = "Number of Gears:";
            // 
            // shifterTable
            // 
            this.shifterTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.shifterTable.Location = new System.Drawing.Point(15, 25);
            this.shifterTable.Name = "shifterTable";
            this.shifterTable.Size = new System.Drawing.Size(851, 252);
            this.shifterTable.TabIndex = 1;
            // 
            // dlGearboxShifterTable
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(878, 519);
            this.Controls.Add(this.shifterTable);
            this.Controls.Add(this.lGearCount);
            this.Name = "dlGearboxShifterTable";
            this.Text = "dlGearboxShifterTable";
            ((System.ComponentModel.ISupportInitialize)(this.shifterTable)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lGearCount;
        private System.Windows.Forms.DataGridView shifterTable;
        private ucGearboxShifterGraph sim;
    }
}