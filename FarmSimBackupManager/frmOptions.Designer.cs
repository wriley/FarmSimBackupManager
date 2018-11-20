namespace FarmSimBackupManager
{
    partial class frmOptions
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
            this.labelBackupFolder = new System.Windows.Forms.Label();
            this.textBoxBackupFolder = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.buttonBackupFolder = new System.Windows.Forms.Button();
            this.buttonOptionsSave = new System.Windows.Forms.Button();
            this.buttonOptionsCancel = new System.Windows.Forms.Button();
            this.comboBoxVersion = new System.Windows.Forms.ComboBox();
            this.labelVersion = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelBackupFolder
            // 
            this.labelBackupFolder.AutoSize = true;
            this.labelBackupFolder.Location = new System.Drawing.Point(12, 15);
            this.labelBackupFolder.Name = "labelBackupFolder";
            this.labelBackupFolder.Size = new System.Drawing.Size(76, 13);
            this.labelBackupFolder.TabIndex = 0;
            this.labelBackupFolder.Text = "Backup Folder";
            // 
            // textBoxBackupFolder
            // 
            this.textBoxBackupFolder.Location = new System.Drawing.Point(94, 12);
            this.textBoxBackupFolder.Name = "textBoxBackupFolder";
            this.textBoxBackupFolder.ReadOnly = true;
            this.textBoxBackupFolder.Size = new System.Drawing.Size(359, 20);
            this.textBoxBackupFolder.TabIndex = 1;
            // 
            // buttonBackupFolder
            // 
            this.buttonBackupFolder.Location = new System.Drawing.Point(459, 10);
            this.buttonBackupFolder.Name = "buttonBackupFolder";
            this.buttonBackupFolder.Size = new System.Drawing.Size(24, 23);
            this.buttonBackupFolder.TabIndex = 2;
            this.buttonBackupFolder.Text = "...";
            this.buttonBackupFolder.UseVisualStyleBackColor = true;
            this.buttonBackupFolder.Click += new System.EventHandler(this.buttonBackupFolder_Click);
            // 
            // buttonOptionsSave
            // 
            this.buttonOptionsSave.Location = new System.Drawing.Point(170, 217);
            this.buttonOptionsSave.Name = "buttonOptionsSave";
            this.buttonOptionsSave.Size = new System.Drawing.Size(75, 23);
            this.buttonOptionsSave.TabIndex = 3;
            this.buttonOptionsSave.Text = "Save";
            this.buttonOptionsSave.UseVisualStyleBackColor = true;
            this.buttonOptionsSave.Click += new System.EventHandler(this.buttonOptionsSave_Click);
            // 
            // buttonOptionsCancel
            // 
            this.buttonOptionsCancel.Location = new System.Drawing.Point(251, 217);
            this.buttonOptionsCancel.Name = "buttonOptionsCancel";
            this.buttonOptionsCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonOptionsCancel.TabIndex = 4;
            this.buttonOptionsCancel.Text = "Cancel";
            this.buttonOptionsCancel.UseVisualStyleBackColor = true;
            this.buttonOptionsCancel.Click += new System.EventHandler(this.buttonOptionsCancel_Click);
            // 
            // comboBoxVersion
            // 
            this.comboBoxVersion.FormattingEnabled = true;
            this.comboBoxVersion.Items.AddRange(new object[] {
            "FarmingSimulator2017",
            "FarmingSimulator2019"});
            this.comboBoxVersion.Location = new System.Drawing.Point(94, 38);
            this.comboBoxVersion.Name = "comboBoxVersion";
            this.comboBoxVersion.Size = new System.Drawing.Size(151, 21);
            this.comboBoxVersion.TabIndex = 5;
            this.comboBoxVersion.SelectedIndexChanged += new System.EventHandler(this.comboBoxVersion_SelectedIndexChanged);
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(46, 41);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(42, 13);
            this.labelVersion.TabIndex = 6;
            this.labelVersion.Text = "Version";
            // 
            // frmOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(495, 262);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.comboBoxVersion);
            this.Controls.Add(this.buttonOptionsCancel);
            this.Controls.Add(this.buttonOptionsSave);
            this.Controls.Add(this.buttonBackupFolder);
            this.Controls.Add(this.textBoxBackupFolder);
            this.Controls.Add(this.labelBackupFolder);
            this.Name = "frmOptions";
            this.Text = "Options";
            this.Load += new System.EventHandler(this.frmOptions_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelBackupFolder;
        private System.Windows.Forms.TextBox textBoxBackupFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button buttonBackupFolder;
        private System.Windows.Forms.Button buttonOptionsSave;
        private System.Windows.Forms.Button buttonOptionsCancel;
        private System.Windows.Forms.ComboBox comboBoxVersion;
        private System.Windows.Forms.Label labelVersion;
    }
}