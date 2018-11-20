using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FarmSimBackupManager
{
    public partial class frmOptions : Form
    {
        frmMain parentForm;

        public frmOptions(frmMain frm)
        {
            InitializeComponent();
            parentForm = new frmMain();
            parentForm = frm;
        }

        private void frmOptions_Load(object sender, EventArgs e)
        {
            textBoxBackupFolder.Text = parentForm.backupFolder;
            comboBoxVersion.SelectedIndex = comboBoxVersion.FindStringExact(parentForm.farmsimVersion);
        }

        private void buttonOptionsSave_Click(object sender, EventArgs e)
        {
            parentForm.SaveSettings();
            Close();
        }

        private void buttonOptionsCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonBackupFolder_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if(result == DialogResult.OK)
            {
                parentForm.backupFolder = folderBrowserDialog1.SelectedPath;
                textBoxBackupFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void comboBoxVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            parentForm.farmsimVersion = comboBoxVersion.SelectedItem.ToString();
        }
    }
}
