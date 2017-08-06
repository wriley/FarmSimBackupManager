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
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;

namespace FarmSimBackupManager
{
    public partial class frmMain : Form
    {
        public string backupFolder;
        private string saveGamePath;
        private string timestampString = "yyyyMMdd-HHmmss";
        private List<TreeNode> unselectableSaveNodes = new List<TreeNode>();
        private List<TreeNode> unselectableBackupNodes = new List<TreeNode>();

        private struct FarmSimSaveGame
        {
            public string directoryName;
            public string savegameName;
            public string mapTitle;
            public string playerName;
            public string saveDate;
            public string backupName;
            public Int32 money;
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
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

            GetSaveGames();
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

        private void GetSaveGames()
        {
            List<FarmSimSaveGame> mySaveGames = new List<FarmSimSaveGame>();
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
                        DebugLog("Found save game directory: " + dirName);
                        string gameXmlFile = dir + Path.DirectorySeparatorChar + "careerSavegame.xml";

                        if (File.Exists(gameXmlFile))
                        {
                            DebugLog("Found XML " + gameXmlFile);
                            FarmSimSaveGame save = new FarmSimSaveGame();

                            XmlDocument gameXml = new XmlDocument();
                            gameXml.Load(gameXmlFile);
                            save.directoryName = new DirectoryInfo(dir).Name;
                            XmlNode node = gameXml.DocumentElement.SelectSingleNode("settings/savegameName");
                            save.savegameName = node.InnerText;
                            node = gameXml.DocumentElement.SelectSingleNode("settings/mapTitle");
                            save.mapTitle = node.InnerText;
                            node = gameXml.DocumentElement.SelectSingleNode("settings/saveDate");
                            save.saveDate = node.InnerText;
                            node = gameXml.DocumentElement.SelectSingleNode("settings/playerName");
                            save.playerName = node.InnerText;
                            node = gameXml.DocumentElement.SelectSingleNode("statistics/money");
                            save.money = Convert.ToInt32(node.InnerText);
                            DebugLog("adding " + save.directoryName);
                            mySaveGames.Add(save);
                        }
                    }
                }
            }

            treeViewSavegames.BeginUpdate();
            treeViewSavegames.Nodes.Clear();
            unselectableSaveNodes.Clear();
            for (int i = 0; i < mySaveGames.Count; i++)
            {
                DebugLog("looking at " + mySaveGames[i].directoryName);
                TreeNode newParentNode = treeViewSavegames.Nodes.Add(mySaveGames[i].directoryName);
                TreeNode newChildNode = newParentNode.Nodes.Add(String.Format("Player: {0}", mySaveGames[i].playerName));
                unselectableSaveNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Name: {0}", mySaveGames[i].savegameName));
                unselectableSaveNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Map: {0}", mySaveGames[i].mapTitle));
                unselectableSaveNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Saved: {0}", mySaveGames[i].saveDate));
                unselectableSaveNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Money: {0:n0}", mySaveGames[i].money));
                unselectableSaveNodes.Add(newChildNode);
            }
            treeViewSavegames.EndUpdate();
        }

        private void GetBackupFiles()
        {
            string[] backupFiles = Directory.GetFiles(backupFolder);
            List<FarmSimSaveGame> backupSaveGames = new List<FarmSimSaveGame>();
            Regex r = new Regex(@"^(savegame[0-9]+)_[0-9]{8}-[0-9]{6}.zip$");
            Match m;
            foreach (string backupFile in backupFiles)
            {
                if(File.Exists(backupFile))
                {
                    string fileName = new FileInfo(backupFile).Name;
                    m = r.Match(fileName);
                    if (m.Success && m.Groups[1].Value != null)
                    {
                        DebugLog("Getting info from zip " + fileName);
                        using (ZipInputStream s = new ZipInputStream(File.OpenRead(backupFile)))
                        {
                            ZipEntry theEntry;
                            while ((theEntry = s.GetNextEntry()) != null)
                            {
                                if (theEntry.Name == "careerSavegame.xml")
                                {
                                    XmlDocument gameXml = new XmlDocument();
                                    FarmSimSaveGame save = new FarmSimSaveGame();
                                    gameXml.Load(s);

                                    save.directoryName = m.Groups[1].Value;
                                    save.backupName = fileName;
                                    XmlNode node = gameXml.DocumentElement.SelectSingleNode("settings/savegameName");
                                    save.savegameName = node.InnerText;
                                    node = gameXml.DocumentElement.SelectSingleNode("settings/mapTitle");
                                    save.mapTitle = node.InnerText;
                                    node = gameXml.DocumentElement.SelectSingleNode("settings/saveDate");
                                    save.saveDate = node.InnerText;
                                    node = gameXml.DocumentElement.SelectSingleNode("settings/playerName");
                                    save.playerName = node.InnerText;
                                    node = gameXml.DocumentElement.SelectSingleNode("statistics/money");
                                    save.money = Convert.ToInt32(node.InnerText);
                                    backupSaveGames.Add(save);
                                }
                            }
                            s.Close();
                        }
                    }
                }
            }
            treeViewBackups.BeginUpdate();
            treeViewBackups.Nodes.Clear();
            unselectableBackupNodes.Clear();
            for (int i = 0; i < backupSaveGames.Count; i++)
            {
                TreeNode newParentNode = treeViewBackups.Nodes.Add(backupSaveGames[i].backupName);
                TreeNode newChildNode = newParentNode.Nodes.Add(String.Format("Player: {0}", backupSaveGames[i].playerName));
                unselectableBackupNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Name: {0}", backupSaveGames[i].savegameName));
                unselectableBackupNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Map: {0}", backupSaveGames[i].mapTitle));
                unselectableBackupNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Saved: {0}", backupSaveGames[i].saveDate));
                unselectableBackupNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Money: {0:n0}", backupSaveGames[i].money));
                unselectableBackupNodes.Add(newChildNode);
            }
            treeViewBackups.EndUpdate();
        }

        private void frmMain_Shown(object sender, EventArgs e)
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

        private void ZipFolder(string sourceDir, string zipFile)
        {
            try
            {
                string[] filenames = Directory.GetFiles(sourceDir);

                using (ZipOutputStream s = new ZipOutputStream(File.Create(zipFile)))
                {
                    s.SetLevel(9);
                    byte[] buffer = new byte[4096];
                    foreach(string file in filenames)
                    {
                        var entry = new ZipEntry(Path.GetFileName(file));
                        entry.DateTime = DateTime.Now;
                        s.PutNextEntry(entry);

                        using(FileStream fs = File.OpenRead(file))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                s.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }
                    s.Finish();
                    s.Close();
                }
            } catch(Exception ex)
            {
                DebugLog("Exception during zip file creation: " + ex.Message);
            }
        }

        private void UnzipFile(string zipFile, string targetDir)
        {
            try
            {
                using( ZipInputStream s = new ZipInputStream(File.OpenRead(zipFile)))
                {
                    ZipEntry theEntry;
                    while((theEntry = s.GetNextEntry()) != null)
                    {
                        string directoryName = Path.GetDirectoryName(theEntry.Name);
                        string fileName = Path.GetFileName(theEntry.Name);

                        if(directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        if(fileName != string.Empty)
                        {
                            using (FileStream streamWriter = File.Create(theEntry.Name))
                            {
                                int size = 2048;
                                byte[] data = new byte[size];
                                while(true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if(size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch(Exception ex)
            {
                DebugLog("Exception while unzipping file: " + ex.Message);
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
                    GetSaveGames();
                }
                else
                {
                    DebugLog("Unable to determine save game folder name from " + dirNameFull);
                }
            }
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

        private void treeViewSavegames_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (unselectableSaveNodes.Contains(e.Node))
            {
                e.Cancel = true;
            }
        }

        private void treeViewBackups_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (unselectableBackupNodes.Contains(e.Node))
            {
                e.Cancel = true;
            }
        }
    }
}
