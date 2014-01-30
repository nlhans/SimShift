namespace SimShift
{
    partial class FrmMain
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSetupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shifterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simpleSetupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.shiftTableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.generalSetupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shifterTableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kickdownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cruiseControlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dynamicCruiseControlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.emergencyStopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gamesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.joystickToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btServiceStartStop = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.modulesToolStripMenuItem,
            this.settingsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(824, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveSetupToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveSetupToolStripMenuItem
            // 
            this.saveSetupToolStripMenuItem.Name = "saveSetupToolStripMenuItem";
            this.saveSetupToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.saveSetupToolStripMenuItem.Text = "Save Setup";
            // 
            // modulesToolStripMenuItem
            // 
            this.modulesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.shifterToolStripMenuItem,
            this.cruiseControlToolStripMenuItem,
            this.dynamicCruiseControlToolStripMenuItem,
            this.emergencyStopToolStripMenuItem});
            this.modulesToolStripMenuItem.Name = "modulesToolStripMenuItem";
            this.modulesToolStripMenuItem.Size = new System.Drawing.Size(65, 20);
            this.modulesToolStripMenuItem.Text = "Modules";
            // 
            // shifterToolStripMenuItem
            // 
            this.shifterToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.simpleSetupToolStripMenuItem,
            this.toolStripSeparator1,
            this.shiftTableToolStripMenuItem,
            this.toolStripSeparator2,
            this.generalSetupToolStripMenuItem,
            this.shifterTableToolStripMenuItem,
            this.kickdownToolStripMenuItem});
            this.shifterToolStripMenuItem.Name = "shifterToolStripMenuItem";
            this.shifterToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.shifterToolStripMenuItem.Text = "Shifter";
            // 
            // simpleSetupToolStripMenuItem
            // 
            this.simpleSetupToolStripMenuItem.Name = "simpleSetupToolStripMenuItem";
            this.simpleSetupToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.simpleSetupToolStripMenuItem.Text = "Simple Setup";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(144, 6);
            // 
            // shiftTableToolStripMenuItem
            // 
            this.shiftTableToolStripMenuItem.Enabled = false;
            this.shiftTableToolStripMenuItem.Name = "shiftTableToolStripMenuItem";
            this.shiftTableToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.shiftTableToolStripMenuItem.Text = "Advanced";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(144, 6);
            // 
            // generalSetupToolStripMenuItem
            // 
            this.generalSetupToolStripMenuItem.Name = "generalSetupToolStripMenuItem";
            this.generalSetupToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.generalSetupToolStripMenuItem.Text = "General Setup";
            this.generalSetupToolStripMenuItem.Click += new System.EventHandler(this.generalSetupToolStripMenuItem_Click);
            // 
            // shifterTableToolStripMenuItem
            // 
            this.shifterTableToolStripMenuItem.Name = "shifterTableToolStripMenuItem";
            this.shifterTableToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.shifterTableToolStripMenuItem.Text = "Shifter Table";
            // 
            // kickdownToolStripMenuItem
            // 
            this.kickdownToolStripMenuItem.Name = "kickdownToolStripMenuItem";
            this.kickdownToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.kickdownToolStripMenuItem.Text = "Kickdown";
            // 
            // cruiseControlToolStripMenuItem
            // 
            this.cruiseControlToolStripMenuItem.Name = "cruiseControlToolStripMenuItem";
            this.cruiseControlToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.cruiseControlToolStripMenuItem.Text = "Static Cruise Control";
            // 
            // dynamicCruiseControlToolStripMenuItem
            // 
            this.dynamicCruiseControlToolStripMenuItem.Name = "dynamicCruiseControlToolStripMenuItem";
            this.dynamicCruiseControlToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.dynamicCruiseControlToolStripMenuItem.Text = "Dynamic Cruise Control";
            // 
            // emergencyStopToolStripMenuItem
            // 
            this.emergencyStopToolStripMenuItem.Name = "emergencyStopToolStripMenuItem";
            this.emergencyStopToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.emergencyStopToolStripMenuItem.Text = "Emergency Stop";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gamesToolStripMenuItem,
            this.joystickToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // gamesToolStripMenuItem
            // 
            this.gamesToolStripMenuItem.Name = "gamesToolStripMenuItem";
            this.gamesToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.gamesToolStripMenuItem.Text = "Games";
            this.gamesToolStripMenuItem.Click += new System.EventHandler(this.gamesToolStripMenuItem_Click);
            // 
            // joystickToolStripMenuItem
            // 
            this.joystickToolStripMenuItem.Name = "joystickToolStripMenuItem";
            this.joystickToolStripMenuItem.Size = new System.Drawing.Size(115, 22);
            this.joystickToolStripMenuItem.Text = "Joystick";
            this.joystickToolStripMenuItem.Click += new System.EventHandler(this.joystickToolStripMenuItem_Click);
            // 
            // btServiceStartStop
            // 
            this.btServiceStartStop.Location = new System.Drawing.Point(12, 27);
            this.btServiceStartStop.Name = "btServiceStartStop";
            this.btServiceStartStop.Size = new System.Drawing.Size(101, 23);
            this.btServiceStartStop.TabIndex = 1;
            this.btServiceStartStop.Text = "Start Service";
            this.btServiceStartStop.UseVisualStyleBackColor = true;
            this.btServiceStartStop.Click += new System.EventHandler(this.btServiceStartStop_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 469);
            this.Controls.Add(this.btServiceStartStop);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FrmMain";
            this.Text = "SimShift";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveSetupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modulesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shifterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cruiseControlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dynamicCruiseControlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem emergencyStopToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gamesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem simpleSetupToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem shiftTableToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem generalSetupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shifterTableToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kickdownToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem joystickToolStripMenuItem;
        private System.Windows.Forms.Button btServiceStartStop;
    }
}

