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
            this.generalSetupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.laneAssistanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gamesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.joystickToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dashboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gamesToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.euroTruckSimulator2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btServiceStartStop = new System.Windows.Forms.Button();
            this.btTransmission = new System.Windows.Forms.Button();
            this.btClutch = new System.Windows.Forms.Button();
            this.gpSim = new System.Windows.Forms.GroupBox();
            this.btSimSelect = new System.Windows.Forms.Button();
            this.btSimMode = new System.Windows.Forms.Button();
            this.cbSimList = new System.Windows.Forms.ComboBox();
            this.lbSimStatus = new System.Windows.Forms.Label();
            this.gbCarSelect = new System.Windows.Forms.GroupBox();
            this.lblCars = new System.Windows.Forms.Label();
            this.btCarApply = new System.Windows.Forms.Button();
            this.cbCars = new System.Windows.Forms.ComboBox();
            this.gbProfiles = new System.Windows.Forms.GroupBox();
            this.lbProfiles = new System.Windows.Forms.ListBox();
            this.gbModules = new System.Windows.Forms.GroupBox();
            this.gbModulesPane = new System.Windows.Forms.Panel();
            this.menuStrip1.SuspendLayout();
            this.gpSim.SuspendLayout();
            this.gbCarSelect.SuspendLayout();
            this.gbProfiles.SuspendLayout();
            this.gbModules.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.modulesToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.dashboardToolStripMenuItem,
            this.gamesToolStripMenuItem1});
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
            this.laneAssistanceToolStripMenuItem});
            this.modulesToolStripMenuItem.Name = "modulesToolStripMenuItem";
            this.modulesToolStripMenuItem.Size = new System.Drawing.Size(65, 20);
            this.modulesToolStripMenuItem.Text = "Modules";
            // 
            // shifterToolStripMenuItem
            // 
            this.shifterToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.generalSetupToolStripMenuItem});
            this.shifterToolStripMenuItem.Name = "shifterToolStripMenuItem";
            this.shifterToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.shifterToolStripMenuItem.Text = "Shifter";
            // 
            // generalSetupToolStripMenuItem
            // 
            this.generalSetupToolStripMenuItem.Name = "generalSetupToolStripMenuItem";
            this.generalSetupToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.generalSetupToolStripMenuItem.Text = "General Setup";
            this.generalSetupToolStripMenuItem.Click += new System.EventHandler(this.generalSetupToolStripMenuItem_Click);
            // 
            // laneAssistanceToolStripMenuItem
            // 
            this.laneAssistanceToolStripMenuItem.Name = "laneAssistanceToolStripMenuItem";
            this.laneAssistanceToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.laneAssistanceToolStripMenuItem.Text = "Lane Assistance";
            this.laneAssistanceToolStripMenuItem.Click += new System.EventHandler(this.laneAssistanceToolStripMenuItem_Click);
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
            // dashboardToolStripMenuItem
            // 
            this.dashboardToolStripMenuItem.Name = "dashboardToolStripMenuItem";
            this.dashboardToolStripMenuItem.Size = new System.Drawing.Size(76, 20);
            this.dashboardToolStripMenuItem.Text = "Dashboard";
            this.dashboardToolStripMenuItem.Click += new System.EventHandler(this.dashboardToolStripMenuItem_Click);
            // 
            // gamesToolStripMenuItem1
            // 
            this.gamesToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.euroTruckSimulator2ToolStripMenuItem});
            this.gamesToolStripMenuItem1.Name = "gamesToolStripMenuItem1";
            this.gamesToolStripMenuItem1.Size = new System.Drawing.Size(55, 20);
            this.gamesToolStripMenuItem1.Text = "Games";
            // 
            // euroTruckSimulator2ToolStripMenuItem
            // 
            this.euroTruckSimulator2ToolStripMenuItem.Name = "euroTruckSimulator2ToolStripMenuItem";
            this.euroTruckSimulator2ToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.euroTruckSimulator2ToolStripMenuItem.Text = "Euro Truck Simulator 2";
            this.euroTruckSimulator2ToolStripMenuItem.Click += new System.EventHandler(this.euroTruckSimulator2ToolStripMenuItem_Click);
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
            // btTransmission
            // 
            this.btTransmission.Location = new System.Drawing.Point(12, 56);
            this.btTransmission.Name = "btTransmission";
            this.btTransmission.Size = new System.Drawing.Size(101, 23);
            this.btTransmission.TabIndex = 2;
            this.btTransmission.Text = "Auto Mode";
            this.btTransmission.UseVisualStyleBackColor = true;
            this.btTransmission.Click += new System.EventHandler(this.btTransmission_Click);
            // 
            // btClutch
            // 
            this.btClutch.Location = new System.Drawing.Point(119, 56);
            this.btClutch.Name = "btClutch";
            this.btClutch.Size = new System.Drawing.Size(96, 23);
            this.btClutch.TabIndex = 3;
            this.btClutch.Text = "Auto Clutch";
            this.btClutch.UseVisualStyleBackColor = true;
            this.btClutch.Click += new System.EventHandler(this.btClutch_Click);
            // 
            // gpSim
            // 
            this.gpSim.Controls.Add(this.btSimSelect);
            this.gpSim.Controls.Add(this.btSimMode);
            this.gpSim.Controls.Add(this.cbSimList);
            this.gpSim.Controls.Add(this.lbSimStatus);
            this.gpSim.Location = new System.Drawing.Point(12, 85);
            this.gpSim.Name = "gpSim";
            this.gpSim.Size = new System.Drawing.Size(273, 100);
            this.gpSim.TabIndex = 4;
            this.gpSim.TabStop = false;
            this.gpSim.Text = "Simulator Select";
            // 
            // btSimSelect
            // 
            this.btSimSelect.Location = new System.Drawing.Point(107, 46);
            this.btSimSelect.Name = "btSimSelect";
            this.btSimSelect.Size = new System.Drawing.Size(78, 23);
            this.btSimSelect.TabIndex = 3;
            this.btSimSelect.Text = "Select";
            this.btSimSelect.UseVisualStyleBackColor = true;
            this.btSimSelect.Click += new System.EventHandler(this.btSimSelect_Click);
            // 
            // btSimMode
            // 
            this.btSimMode.Location = new System.Drawing.Point(191, 46);
            this.btSimMode.Name = "btSimMode";
            this.btSimMode.Size = new System.Drawing.Size(76, 23);
            this.btSimMode.TabIndex = 2;
            this.btSimMode.Text = "Auto";
            this.btSimMode.UseVisualStyleBackColor = true;
            this.btSimMode.Click += new System.EventHandler(this.btSimMode_Click);
            // 
            // cbSimList
            // 
            this.cbSimList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSimList.FormattingEnabled = true;
            this.cbSimList.Location = new System.Drawing.Point(9, 19);
            this.cbSimList.Name = "cbSimList";
            this.cbSimList.Size = new System.Drawing.Size(258, 21);
            this.cbSimList.TabIndex = 1;
            // 
            // lbSimStatus
            // 
            this.lbSimStatus.AutoSize = true;
            this.lbSimStatus.Location = new System.Drawing.Point(6, 66);
            this.lbSimStatus.Name = "lbSimStatus";
            this.lbSimStatus.Size = new System.Drawing.Size(25, 13);
            this.lbSimStatus.TabIndex = 0;
            this.lbSimStatus.Text = "app";
            // 
            // gbCarSelect
            // 
            this.gbCarSelect.Controls.Add(this.lblCars);
            this.gbCarSelect.Controls.Add(this.btCarApply);
            this.gbCarSelect.Controls.Add(this.cbCars);
            this.gbCarSelect.Location = new System.Drawing.Point(12, 191);
            this.gbCarSelect.Name = "gbCarSelect";
            this.gbCarSelect.Size = new System.Drawing.Size(273, 89);
            this.gbCarSelect.TabIndex = 5;
            this.gbCarSelect.TabStop = false;
            this.gbCarSelect.Text = "Car Select";
            // 
            // lblCars
            // 
            this.lblCars.AutoSize = true;
            this.lblCars.Location = new System.Drawing.Point(6, 73);
            this.lblCars.Name = "lblCars";
            this.lblCars.Size = new System.Drawing.Size(22, 13);
            this.lblCars.TabIndex = 2;
            this.lblCars.Text = "car";
            // 
            // btCarApply
            // 
            this.btCarApply.Location = new System.Drawing.Point(191, 46);
            this.btCarApply.Name = "btCarApply";
            this.btCarApply.Size = new System.Drawing.Size(76, 23);
            this.btCarApply.TabIndex = 1;
            this.btCarApply.Text = "Apply";
            this.btCarApply.UseVisualStyleBackColor = true;
            this.btCarApply.Click += new System.EventHandler(this.btCarApply_Click);
            // 
            // cbCars
            // 
            this.cbCars.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCars.FormattingEnabled = true;
            this.cbCars.Location = new System.Drawing.Point(6, 19);
            this.cbCars.Name = "cbCars";
            this.cbCars.Size = new System.Drawing.Size(261, 21);
            this.cbCars.TabIndex = 0;
            // 
            // gbProfiles
            // 
            this.gbProfiles.Controls.Add(this.lbProfiles);
            this.gbProfiles.Location = new System.Drawing.Point(12, 286);
            this.gbProfiles.Name = "gbProfiles";
            this.gbProfiles.Size = new System.Drawing.Size(273, 117);
            this.gbProfiles.TabIndex = 6;
            this.gbProfiles.TabStop = false;
            this.gbProfiles.Text = "Profile Active";
            // 
            // lbProfiles
            // 
            this.lbProfiles.FormattingEnabled = true;
            this.lbProfiles.Location = new System.Drawing.Point(6, 19);
            this.lbProfiles.Name = "lbProfiles";
            this.lbProfiles.Size = new System.Drawing.Size(261, 82);
            this.lbProfiles.TabIndex = 7;
            // 
            // gbModules
            // 
            this.gbModules.Controls.Add(this.gbModulesPane);
            this.gbModules.Location = new System.Drawing.Point(291, 85);
            this.gbModules.Name = "gbModules";
            this.gbModules.Size = new System.Drawing.Size(473, 318);
            this.gbModules.TabIndex = 7;
            this.gbModules.TabStop = false;
            this.gbModules.Text = "Module Controls";
            // 
            // gbModulesPane
            // 
            this.gbModulesPane.BackColor = System.Drawing.Color.Black;
            this.gbModulesPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbModulesPane.Location = new System.Drawing.Point(3, 16);
            this.gbModulesPane.Name = "gbModulesPane";
            this.gbModulesPane.Size = new System.Drawing.Size(467, 299);
            this.gbModulesPane.TabIndex = 0;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 469);
            this.Controls.Add(this.gbModules);
            this.Controls.Add(this.gbProfiles);
            this.Controls.Add(this.gbCarSelect);
            this.Controls.Add(this.gpSim);
            this.Controls.Add(this.btClutch);
            this.Controls.Add(this.btTransmission);
            this.Controls.Add(this.btServiceStartStop);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FrmMain";
            this.Text = "SimShift";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.gpSim.ResumeLayout(false);
            this.gpSim.PerformLayout();
            this.gbCarSelect.ResumeLayout(false);
            this.gbCarSelect.PerformLayout();
            this.gbProfiles.ResumeLayout(false);
            this.gbModules.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveSetupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modulesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shifterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gamesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem generalSetupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem joystickToolStripMenuItem;
        private System.Windows.Forms.Button btServiceStartStop;
        private System.Windows.Forms.ToolStripMenuItem laneAssistanceToolStripMenuItem;
        private System.Windows.Forms.Button btTransmission;
        private System.Windows.Forms.ToolStripMenuItem dashboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gamesToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem euroTruckSimulator2ToolStripMenuItem;
        private System.Windows.Forms.Button btClutch;
        private System.Windows.Forms.GroupBox gpSim;
        private System.Windows.Forms.Button btSimMode;
        private System.Windows.Forms.ComboBox cbSimList;
        private System.Windows.Forms.Label lbSimStatus;
        private System.Windows.Forms.Button btSimSelect;
        private System.Windows.Forms.GroupBox gbCarSelect;
        private System.Windows.Forms.ComboBox cbCars;
        private System.Windows.Forms.Button btCarApply;
        private System.Windows.Forms.Label lblCars;
        private System.Windows.Forms.GroupBox gbProfiles;
        private System.Windows.Forms.ListBox lbProfiles;
        private System.Windows.Forms.GroupBox gbModules;
        private System.Windows.Forms.Panel gbModulesPane;
    }
}

