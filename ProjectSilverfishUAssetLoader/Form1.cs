using System;
using System.Reflection;
using System.Windows.Forms;
using UAssetMergerLibrary;

namespace ProjectSilverfishUAssetLoader
{

    public partial class Form1 : Form
    {
        readonly string scriptPath = AppContext.BaseDirectory;
        readonly string modOrderPath = Path.Combine(AppContext.BaseDirectory, "scriptModOrder.txt");
        readonly string replacePath = Path.Combine(AppContext.BaseDirectory, "scriptReplaceList.txt");
        readonly string historyPath = Path.Combine(AppContext.BaseDirectory, "scriptReplaceHistory.txt");
        readonly string backupRoot = Path.Combine(AppContext.BaseDirectory, "Backups");
        readonly string generatedRoot = Path.Combine(AppContext.BaseDirectory, "Generated");
        readonly string gameDirectoryName = "Project Silverfish";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnCleanBackups_Click(object sender, EventArgs e)
        {
            Directory.Delete(backupRoot, true);
            File.Delete(historyPath);
        }

        private void btnCleanGenerated_Click(object sender, EventArgs e)
        {
            Directory.Delete(generatedRoot, true);
            File.Delete(replacePath);
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(backupRoot))
                return;
            string? targetRoot = null;
            DirectoryInfo? current = new(scriptPath);
            while (current != null)
            {
                if (current.Name.Equals(gameDirectoryName))
                {
                    targetRoot = current.FullName;
                    break;
                }
                current = current.Parent;
            }
            if (targetRoot == null)
                return;
            if (!File.Exists(historyPath))
            {
                foreach (string sourceFile in Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(backupRoot, sourceFile);
                    string targetFile = Path.Combine(targetRoot, relativePath);
                    File.Copy(sourceFile, targetFile, true);
                }
            }
            else
            {
                foreach (string line in File.ReadAllLines(historyPath))
                {
                    string[] split = line.Split("|!|");
                    if (split[0] == "added")
                    {
                        File.Delete(split[1]);
                    }
                }
                foreach (string sourceFile in Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(backupRoot, sourceFile);
                    string targetFile = Path.Combine(targetRoot, relativePath);
                    File.Copy(sourceFile, targetFile, true);
                }
            }
        }

        private void btnOverwrite_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(generatedRoot))
                return;
            if (File.Exists(historyPath))
                return;
            string? targetRoot = null;
            DirectoryInfo? current = new(scriptPath);
            while (current != null)
            {
                if (current.Name.Equals(gameDirectoryName))
                {
                    targetRoot = current.FullName;
                    break;
                }
                current = current.Parent;
            }
            if (targetRoot == null)
                return;
            foreach (string sourceFile in Directory.GetFiles(generatedRoot, "*", SearchOption.AllDirectories))
            {
                if (sourceFile.Contains("scriptfinalhash") || sourceFile.Contains("scriptbak"))
                    continue;
                string relativePath = Path.GetRelativePath(generatedRoot, sourceFile);
                string targetFile = Path.Combine(targetRoot, relativePath);
                string backupFile = Path.Combine(backupRoot, relativePath);
                string? targetDirectory = Path.GetDirectoryName(targetFile);
                if (targetDirectory != null)
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                string? backupDirectory = Path.GetDirectoryName(backupFile);
                if (backupDirectory != null)
                {
                    Directory.CreateDirectory(backupDirectory);
                }
                if (File.Exists(targetFile))
                {
                    File.Copy(targetFile, backupFile, true);
                    File.Copy(sourceFile, targetFile, true);
                    File.AppendAllLines(historyPath, [$"replaced|!|{targetFile}"]);
                }
                else
                {
                    File.Copy(sourceFile, targetFile, true);
                    File.AppendAllLines(historyPath, [$"added|!|{targetFile}"]);
                }
            }
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (File.Exists(modOrderPath))
                UAssetMerger.Run();
        }

        private void btnLoadModOrder_Click(object sender, EventArgs e)
        {
            string sourceRoot = Path.Combine(scriptPath, "Mods");
            Directory.CreateDirectory(sourceRoot);
            string[] modFolders = Directory.GetDirectories(sourceRoot);
            List<(string, bool)> modOrder = [];
            List<string> mods = [];
            if (File.Exists(modOrderPath))
            {
                string[] modOrderFile = File.ReadAllLines(modOrderPath);
                foreach (string mod in modOrderFile)
                {
                    if (mod.Contains(" = "))
                    {
                        string[] split = mod.Split(" = ");
                        if (Directory.Exists(split[0]))
                        {
                            mods.Add(split[0]);
                            modOrder.Add((split[0], (split[1].Contains('1') || split[1].Contains("True"))));
                        }
                    }
                }
            }
            foreach (string modFolder in modFolders)
                if (!mods.Contains(modFolder))
                    modOrder.Add((modFolder, false));
            lbModOrder.Items.Clear();
            List<string> toWrite = [];
            foreach ((string modName, bool modOn) in modOrder)
            {
                toWrite.Add($"{modName} = {modOn}");
                lbModOrder.Items.Add(new ModPath($"{modName}"), modOn);
            }
            File.WriteAllLines(modOrderPath, toWrite);
        }

        private void lbModOrder_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<string> toWrite = [];
            foreach (var item in lbModOrder.Items.Cast<ModPath>().ToArray())
            {
                toWrite.Add($"{item.FullPath} = {lbModOrder.CheckedItems.Contains(item)}");
            }
            File.WriteAllLines(modOrderPath, toWrite);
        }

        private void btnUpButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = lbModOrder.SelectedIndex;
            if (selectedIndex > 0)
            {
                var item = lbModOrder.Items[selectedIndex];
                bool isChecked = lbModOrder.GetItemChecked(selectedIndex);

                var aboveItem = lbModOrder.Items[selectedIndex - 1];
                bool aboveChecked = lbModOrder.GetItemChecked(selectedIndex - 1);

                // Swap
                lbModOrder.Items[selectedIndex - 1] = item;
                lbModOrder.Items[selectedIndex] = aboveItem;

                // Restore checked state
                lbModOrder.SetItemChecked(selectedIndex - 1, isChecked);
                lbModOrder.SetItemChecked(selectedIndex, aboveChecked);

                lbModOrder.SelectedIndex = selectedIndex - 1;
            }
            List<string> toWrite = [];
            foreach (var item in lbModOrder.Items.Cast<ModPath>().ToArray())
            {
                toWrite.Add($"{item.FullPath} = {lbModOrder.CheckedItems.Contains(item)}");
            }
            File.WriteAllLines(modOrderPath, toWrite);
        }

        private void btnDownButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = lbModOrder.SelectedIndex;

            if (selectedIndex >= 0 && selectedIndex < lbModOrder.Items.Count - 1)
            {
                var item = lbModOrder.Items[selectedIndex];
                bool isChecked = lbModOrder.GetItemChecked(selectedIndex);

                var belowItem = lbModOrder.Items[selectedIndex + 1];
                bool belowChecked = lbModOrder.GetItemChecked(selectedIndex + 1);

                lbModOrder.Items[selectedIndex + 1] = item;
                lbModOrder.Items[selectedIndex] = belowItem;

                lbModOrder.SetItemChecked(selectedIndex + 1, isChecked);
                lbModOrder.SetItemChecked(selectedIndex, belowChecked);

                lbModOrder.SelectedIndex = selectedIndex + 1;
            }
            List<string> toWrite = [];
            foreach (var item in lbModOrder.Items.Cast<ModPath>().ToArray())
            {
                toWrite.Add($"{item.FullPath} = {lbModOrder.CheckedItems.Contains(item)}");
            }
            File.WriteAllLines(modOrderPath, toWrite);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!Directory.Exists(backupRoot))
            {
                btnRestore.Enabled = false;
                btnCleanBackups.Enabled = false;
            }
            else
            {
                btnRestore.Enabled = true;
                btnCleanBackups.Enabled = true;
            }
            if (Directory.Exists(generatedRoot))
            {
                btnCleanGenerated.Enabled = true;
                btnOverwrite.Enabled = true;
            }
            else
            {
                btnCleanGenerated.Enabled = false;
                btnOverwrite.Enabled = false;
            }
            if (File.Exists(modOrderPath) && !Directory.Exists(generatedRoot))
                btnGenerate.Enabled = true;
            else
                btnGenerate.Enabled = false;
        }
    }
    public class ModPath
    {
        public string FullPath { get; }

        public ModPath(string fullPath)
        {
            FullPath = fullPath;
        }

        public override string ToString()
        {
            return Path.GetFileName(FullPath); // or use Path.GetFileNameWithoutExtension if needed
        }
    }
}
