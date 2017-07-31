using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace FarmSimBackupManager
{
    public partial class frmMain : Form
    {
        public string backupFolder;
        private string saveGamePath;
        private string timestampString = "yyyyMMdd-HHmmss";

        public frmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSettings();

            if (!Directory.Exists(backupFolder))
            {
                MessageBox.Show("You need to set your backup folder");
                frmOptions frmOptions = new frmOptions(this);
                frmOptions.Show(this);
            }

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DebugLog("Found My Documents path: " + path);
            // C:\Users\Computer User\Documents\My Games\FarmingSimulator2017
            saveGamePath = path + Path.DirectorySeparatorChar + "My Games" + Path.DirectorySeparatorChar + "FarmingSimulator2017";

            GetSaveGameDirs();
            GetBackupFiles();
        }

        private void LoadSettings()
        {
            backupFolder = Properties.Settings.Default.backupFolder;
        }

        public void SaveSettings()
        {
            Properties.Settings.Default.backupFolder = backupFolder;
            Properties.Settings.Default.Save();
            GetBackupFiles();
        }

        private void DebugLog(string msg)
        {
            textBoxDebug.AppendText(msg + "\r\n");
        }

        private void GetSaveGameDirs()
        {
            List<string> mySaveGameDirs = new List<string>();
            string[] dirs = Directory.GetDirectories(saveGamePath);
            foreach (string dir in dirs)
            {
                //DebugLog("Examining: " + dir);
                if (Directory.Exists(dir))
                {
                    string dirName = new DirectoryInfo(dir).Name;
                    Regex r = new Regex("^savegame[0-9]+$");
                    Match m = r.Match(dirName);
                    if (m.Success)
                    {
                        DebugLog("Found save game direcory: " + dirName);
                        mySaveGameDirs.Add(dir);
                    }
                }
            }
            mySaveGameDirs.Sort();
            treeViewSavegames.Nodes.Clear();
            foreach (string mySaveGameDir in mySaveGameDirs)
            {
                string dirName = new DirectoryInfo(mySaveGameDir).Name;
                treeViewSavegames.Nodes.Add(dirName);
            }
        }

        private void GetBackupFiles()
        {
            string[] backupFiles = Directory.GetFiles(backupFolder);
            treeViewBackups.Nodes.Clear();
            Regex r = new Regex("^savegame[0-9]+_[0-9]{8}-[0-9]{6}.zip$");
            Match m;
            foreach (string backupFile in backupFiles)
            {
                if(File.Exists(backupFile))
                {
                    string fileName = new FileInfo(backupFile).Name;
                    m = r.Match(fileName);
                    if (m.Success)
                    {
                        treeViewBackups.Nodes.Add(fileName);
                    }
                }
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            textBoxDebug.SelectionStart = textBoxDebug.Text.Length;
            textBoxDebug.ScrollToCaret();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmOptions frmOptions = new frmOptions(this);
            frmOptions.Show(this);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAbout frmAbout = new frmAbout();
            frmAbout.ShowDialog(this);
        }

        private void buttonBackup_Click(object sender, EventArgs e)
        {
            if (treeViewSavegames.SelectedNode != null)
            {
                string dirName = treeViewSavegames.SelectedNode.Text;
                SaveGame(dirName);
            }
        }

        private void buttonRestore_Click(object sender, EventArgs e)
        {
            if(treeViewBackups.SelectedNode != null)
            {
                string backupName = treeViewBackups.SelectedNode.Text;
                RestoreGame(backupName);
            }
        }

        private void SaveGame(string dirName)
        {
            DebugLog("Saving game " + dirName);
            string mySaveGameDir = saveGamePath + Path.DirectorySeparatorChar + dirName;
            if (Directory.Exists(mySaveGameDir))
            {
                string dateString = DateTime.Now.ToString(timestampString);
                string zipFilePath = backupFolder + Path.DirectorySeparatorChar + dirName + "_" + dateString + ".zip";
                DebugLog("zipping to " + zipFilePath);
                ZipFolder(mySaveGameDir, zipFilePath);
                GetBackupFiles();
            }
        }

        private void RestoreGame(string backupName)
        {
            DebugLog("Restoring game " + backupName);
            string dirNameFull = new FileInfo(backupName).Name;
            Regex r = new Regex(@"(savegame[0-9]+)_[0-9]{8}-[0-9]{6}.zip");
            Match m = r.Match(dirNameFull);
            DebugLog("dirNameFull " + dirNameFull);
            if (m.Success)
            {
                if (m.Groups[1].Value != null)
                {
                    string dirName = m.Groups[1].Value;
                    string mySaveGameDir = saveGamePath + Path.DirectorySeparatorChar + dirName;
                    if (Directory.Exists(mySaveGameDir))
                    {
                        DebugLog(dirName + " already exists!");
                        DialogResult result = MessageBox.Show(dirName + " already exists, overwrite?", "Overwrite Save?", MessageBoxButtons.YesNo);
                        if(result == DialogResult.Yes)
                        {
                            Directory.Delete(mySaveGameDir, true);
                        }
                        if(result == DialogResult.No)
                        {
                            return;
                        }
                    }
                    string zipFilePath = backupFolder + Path.DirectorySeparatorChar + backupName;
                    DebugLog("Unzipping from " + zipFilePath);
                    UnzipFile(zipFilePath, mySaveGameDir);
                    GetSaveGameDirs();
                }
                else
                {
                    DebugLog("Unable to determine save game folder name from " + dirNameFull);
                }
            }
        }

        public static void ZipFolder(string sourceFolder, string zipFile)
        {
            if (!System.IO.Directory.Exists(sourceFolder))
                throw new ArgumentException("sourceDirectory");

            byte[] zipHeader = new byte[] { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            using (System.IO.FileStream fs = System.IO.File.Create(zipFile))
            {
                fs.Write(zipHeader, 0, zipHeader.Length);
            }

            dynamic shellApplication = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
            dynamic source = shellApplication.NameSpace(sourceFolder);
            dynamic destination = shellApplication.NameSpace(zipFile);

            destination.CopyHere(source.Items(), 20);
        }

        public static void UnzipFile(string zipFile, string targetFolder)
        {
            if (!System.IO.Directory.Exists(targetFolder))
                System.IO.Directory.CreateDirectory(targetFolder);

            dynamic shellApplication = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
            dynamic compressedFolderContents = shellApplication.NameSpace(zipFile).Items;
            dynamic destinationFolder = shellApplication.NameSpace(targetFolder);

            destinationFolder.CopyHere(compressedFolderContents);
        }

        private void buttonRemoveBackup_Click(object sender, EventArgs e)
        {
            if (treeViewBackups.SelectedNode != null)
            {
                string backupName = treeViewBackups.SelectedNode.Text;
                string zipFilePath = backupFolder + Path.DirectorySeparatorChar + backupName;
                if(File.Exists(zipFilePath))
                {
                    DialogResult result = MessageBox.Show("Remove backup file " + backupName + "?", "Remove backup?", MessageBoxButtons.YesNo);
                    if(result == DialogResult.Yes)
                    {
                        File.Delete(zipFilePath);
                        GetBackupFiles();
                    }
                }
            }
        }
    }
}
