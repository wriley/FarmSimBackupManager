using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using System.Globalization;
using System.Linq;

namespace FarmSimBackupManager
{
    public partial class frmMain : Form
    {
        public string backupFolder;
        public string farmsimVersion;
        private string saveGamePath;
        private string saveGamePathRoot;
        private string timestampString = "yyyyMMdd-HHmmss";
        private List<TreeNode> unselectableSaveNodes = new List<TreeNode>();
        private List<TreeNode> unselectableBackupNodes = new List<TreeNode>();
        private List<FarmSimSaveGame> mySaveGames = new List<FarmSimSaveGame>();
        private List<FarmSimSaveGame> backupSaveGames = new List<FarmSimSaveGame>();

        private struct FarmSimSaveGame
        {
            public string directoryName;
            public DateTime directoryChanged;
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

            if (!Directory.Exists(backupFolder) || farmsimVersion == "")
            {
                farmsimVersion = "FarmingSimulator2019";
                MessageBox.Show("You need to set your options");
                frmOptions frmOptions = new frmOptions(this);
                frmOptions.Show(this);
            }

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DebugLog("Found My Documents path: " + path);
            // C:\Users\Computer User\Documents\My Games\FarmingSimulator2017
            saveGamePathRoot = path + Path.DirectorySeparatorChar + "My Games";
            saveGamePath = saveGamePathRoot + Path.DirectorySeparatorChar + farmsimVersion;

            RefreshLists();
        }

        private void LoadSettings()
        {
            backupFolder = Properties.Settings.Default.backupFolder;
            farmsimVersion = Properties.Settings.Default.version;
        }

        public void SaveSettings()
        {
            DebugLog("version: " + farmsimVersion);
            Properties.Settings.Default.backupFolder = backupFolder;
            Properties.Settings.Default.version = farmsimVersion;
            Properties.Settings.Default.Save();
            saveGamePath = saveGamePathRoot + Path.DirectorySeparatorChar + farmsimVersion;
            GetBackupFiles();
            RefreshLists();
        }

        private void DebugLog(string msg)
        {
            textBoxDebug.AppendText(msg + "\r\n");
        }

        private void GetSaveGames()
        {
            if(!Directory.Exists(saveGamePath))
            {
                DebugLog("Save game path not found, check options!");
                return;
            }
            mySaveGames = new List<FarmSimSaveGame>();
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
                            DirectoryInfo di = new DirectoryInfo(dir);
                            save.directoryName = di.Name;
                            save.directoryChanged = di.LastWriteTime;
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
            mySaveGames = mySaveGames.OrderBy(sel => sel.directoryName, new OrdinalStringComparer()).ToList();
            treeViewSavegames.BeginUpdate();
            treeViewSavegames.Nodes.Clear();
            unselectableSaveNodes.Clear();
            for (int i = 0; i < mySaveGames.Count; i++)
            {
                DebugLog("looking at " + mySaveGames[i].directoryName);
                TreeNode newParentNode = treeViewSavegames.Nodes.Add(String.Format("{0}: {1} ({2})", mySaveGames[i].directoryName, mySaveGames[i].savegameName, mySaveGames[i].directoryChanged.ToString()));
                TreeNode newChildNode = newParentNode.Nodes.Add(String.Format("Player: {0}", mySaveGames[i].playerName));
                unselectableSaveNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Map: {0}", mySaveGames[i].mapTitle));
                unselectableSaveNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Saved: {0}", mySaveGames[i].saveDate));
                unselectableSaveNodes.Add(newChildNode);
                newChildNode = newParentNode.Nodes.Add(String.Format("Money: {0:n0}", mySaveGames[i].money));
                unselectableSaveNodes.Add(newChildNode);

                DateTime latest = GetLatestZipDate(mySaveGames[i].directoryName);
                if(latest.CompareTo(mySaveGames[i].directoryChanged) >= 0)
                {
                    newParentNode.ForeColor = System.Drawing.Color.Green;
                    //newParentNode.NodeFont = new System.Drawing.Font(treeViewBackups.Font, System.Drawing.FontStyle.Bold);
                }
            }
            treeViewSavegames.EndUpdate();
        }

        private void GetBackupFiles()
        {
            if (!Directory.Exists(backupFolder))
            {
                DebugLog("Backup folder not found, check options!");
                return;
            }
            string[] backupFiles = Directory.GetFiles(backupFolder);
            backupSaveGames = new List<FarmSimSaveGame>();
            Regex r = new Regex(@"^" + farmsimVersion + "_(savegame[0-9]+)_[0-9]{8}-[0-9]{6}.zip$");
            Match m;
            foreach (string backupFile in backupFiles)
            {
                if (File.Exists(backupFile))
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
            backupSaveGames = backupSaveGames.OrderBy(sel => sel.directoryName, new OrdinalStringComparer()).ToList();
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

        private DateTime GetSaveGameDate(string save)
        {
            for (int i = 0; i < mySaveGames.Count; i++)
            {
                if (mySaveGames[i].directoryName == save)
                {
                    return mySaveGames[i].directoryChanged;
                }
            }
            return DateTime.MinValue;
        }

        private DateTime GetLatestZipDate(string save)
        {
            DateTime latest = DateTime.MinValue;
            DateTime lastDate = DateTime.MinValue;
            for (int i = 0; i < backupSaveGames.Count; i++)
            {
                Regex r = new Regex(@"^" + farmsimVersion + "_(savegame[0-9]+)_([0-9]{8}-[0-9]{6}).zip$");
                Match m = r.Match(backupSaveGames[i].backupName);
                if (m.Success)
                {
                    if (m.Groups[1].Value == save)
                    {
                        DateTime zipDate = new DateTime();
                        DateTime.TryParseExact(m.Groups[2].Value, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out zipDate);
                        if (zipDate != DateTime.MinValue && zipDate.CompareTo(lastDate) == 1)
                        {
                            latest = zipDate;
                        }
                    }
                }
            }
            return latest;
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
            frmOptions.ShowDialog(this);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAbout frmAbout = new frmAbout();
            frmAbout.ShowDialog(this);
        }

        private void buttonBackup_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(backupFolder))
            {
                DebugLog("Backup folder not found, check options!");
                return;
            }
            if (treeViewSavegames.SelectedNode != null)
            {

                string dirText = treeViewSavegames.SelectedNode.Text;
                int i = dirText.IndexOf(':');
                string dirName = dirText.Substring(0, i);
                showUI(false);
                SaveGame(dirName);
                showUI(true);
                RefreshLists();
            }
            else
            {
                DebugLog("No save game selected to backup!");
            }
        }

        private void buttonRestore_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(saveGamePath))
            {
                DebugLog("Save game path not found, check options!");
                return;
            }
            if (treeViewBackups.SelectedNode != null)
            {
                string backupName = treeViewBackups.SelectedNode.Text;
                showUI(false);
                RestoreGame(backupName);
                showUI(true);
                RefreshLists();
            }
            else
            {
                DebugLog("No backup file selected to restore!");
            }
        }

        private void SaveGame(string dirName)
        {
            DebugLog("Saving game " + dirName);
            string mySaveGameDir = saveGamePath + Path.DirectorySeparatorChar + dirName;
            if (Directory.Exists(mySaveGameDir))
            {
                string dateString = DateTime.Now.ToString(timestampString);
                string zipFilePath = backupFolder + Path.DirectorySeparatorChar + farmsimVersion + "_" + dirName + "_" + dateString + ".zip";
                DebugLog("zipping to " + zipFilePath);
                ZipFolder(mySaveGameDir, zipFilePath);
                GetBackupFiles();
                DebugLog("SaveGame complete");
            }
            else
            {
                DebugLog("Error: Directory not found " + mySaveGameDir);
            }
        }

        private void ZipFolder(string sourceDir, string zipFile)
        {
            try
            {
                string[] filenames = Directory.GetFiles(sourceDir);

                using (ZipOutputStream s = new ZipOutputStream(File.Create(zipFile)))
                {
                    s.SetLevel(3);
                    byte[] buffer = new byte[4096];
                    foreach (string file in filenames)
                    {
                        var entry = new ZipEntry(Path.GetFileName(file));
                        entry.DateTime = DateTime.Now;
                        s.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(file))
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
            }
            catch (Exception ex)
            {
                DebugLog("Exception during zip file creation: " + ex.Message);
            }
        }

        private void UnzipFile(string zipFile, string targetDir)
        {
            try
            {
                using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFile)))
                {
                    ZipEntry theEntry;
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        string directoryName = Path.GetDirectoryName(theEntry.Name);
                        string fileName = Path.GetFileName(theEntry.Name);

                        if (directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(Path.Combine(targetDir, directoryName));
                        }

                        if (fileName != string.Empty)
                        {
                            var filePath = Path.Combine(targetDir, fileName);
                            using (FileStream streamWriter = File.Create(filePath))
                            {
                                int size = 2048;
                                byte[] data = new byte[size];
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
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
            }
            catch (Exception ex)
            {
                DebugLog("Exception while unzipping file: " + ex.Message);
            }
        }

        private void RestoreGame(string backupName)
        {
            DebugLog("Restoring game " + backupName);
            string dirNameFull = new FileInfo(backupName).Name;
            Regex r = new Regex(@"^" + farmsimVersion + "_(savegame[0-9]+)_[0-9]{8}-[0-9]{6}.zip$");
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
                        if (result == DialogResult.Yes)
                        {
                            // file system calls can be delayed so wait for folder to be deleted
                            var fi = new System.IO.FileInfo(mySaveGameDir);
                            if (fi.Exists)
                            {
                                Directory.Delete(mySaveGameDir, true);
                                fi.Refresh();
                                while(fi.Exists)
                                {
                                    System.Threading.Thread.Sleep(100);
                                    fi.Refresh();
                                }
                            }
                        }
                        if (result == DialogResult.No)
                        {
                            return;
                        }
                    }
                    Directory.CreateDirectory(mySaveGameDir);
                    string zipFilePath = backupFolder + Path.DirectorySeparatorChar + backupName;
                    DebugLog("Unzipping from " + zipFilePath);
                    UnzipFile(zipFilePath, mySaveGameDir);
                    GetSaveGames();
                }
                else
                {
                    DebugLog("Unable to determine save game folder name from " + dirNameFull);
                }
                DebugLog("RestoreGame complete");
            }
        }

        private void buttonRemoveBackup_Click(object sender, EventArgs e)
        {
            if (treeViewBackups.SelectedNode != null)
            {
                string backupName = treeViewBackups.SelectedNode.Text;
                string zipFilePath = backupFolder + Path.DirectorySeparatorChar + backupName;
                if (File.Exists(zipFilePath))
                {
                    DialogResult result = MessageBox.Show("Remove backup file " + backupName + "?", "Remove backup?", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        File.Delete(zipFilePath);
                        GetBackupFiles();
                        DebugLog("RemoveBackup complete");
                    }
                }
            }
            else
            {
                DebugLog("No backup selected to remove!");
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

        private void showUI(bool show)
        {
            treeViewSavegames.Enabled = show;
            treeViewBackups.Enabled = show;
            buttonBackup.Enabled = show;
            buttonRestore.Enabled = show;
            buttonRemoveBackup.Enabled = show;
            buttonOpenBackupLocation.Enabled = show;
            buttonRefresh.Enabled = show;
        }

        // https://www.codeproject.com/Questions/852563/How-to-open-file-explorer-at-given-location-in-csh
        private void OpenFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("explorer.exe");
                startInfo.Arguments = folderPath;

                Process.Start(startInfo);
            }
            else
            {
                MessageBox.Show(string.Format("{0} Directory does not exist!", folderPath));
            }
        }

        private void buttonOpenBackupLocation_Click(object sender, EventArgs e)
        {
            OpenFolder(backupFolder);
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            RefreshLists();
        }

        private void RefreshLists()
        {
            showUI(false);
            GetBackupFiles();
            GetSaveGames();
            showUI(true);
        }
    }

    // https://code.msdn.microsoft.com/windowsdesktop/Ordinal-String-Sorting-1cbac582
    public class OrdinalStringComparer : IComparer<string>
    {
        private bool _ignoreCase = true;

        /// <summary>
        /// Creates an instance of <c>OrdinalStringComparer</c> for case-insensitive string comparison.
        /// </summary>
        public OrdinalStringComparer()
            : this(true)
        {
        }

        /// <summary>
        /// Creates an instance of <c>OrdinalStringComparer</c> for case comparison according to the value specified in input.
        /// </summary>
        /// <param name="ignoreCase">true to ignore case during the comparison; otherwise, false.</param>
        public OrdinalStringComparer(bool ignoreCase)
        {
            _ignoreCase = ignoreCase;
        }

        /// <summary>
        /// Compares two strings and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first string to compare.</param>
        /// <param name="y">The second string to compare.</param>
        /// <returns>A signed integer that indicates the relative values of x and y, as in the Compare method in the <c>IComparer&lt;T&gt;</c> interface.</returns>
        public int Compare(string x, string y)
        {
            // check for null values first: a null reference is considered to be less than any reference that is not null
            if (x == null && y == null)
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            StringComparison comparisonMode = _ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;

            string[] splitX = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
            string[] splitY = Regex.Split(y.Replace(" ", ""), "([0-9]+)");

            int comparer = 0;

            for (int i = 0; comparer == 0 && i < splitX.Length; i++)
            {
                if (splitY.Length <= i)
                {
                    comparer = 1; // x > y
                }

                int numericX = -1;
                int numericY = -1;
                if (int.TryParse(splitX[i], out numericX))
                {
                    if (int.TryParse(splitY[i], out numericY))
                    {
                        comparer = numericX - numericY;
                    }
                    else
                    {
                        comparer = 1; // x > y
                    }
                }
                else
                {
                    comparer = String.Compare(splitX[i], splitY[i], comparisonMode);
                }
            }

            return comparer;
        }
    }
}
