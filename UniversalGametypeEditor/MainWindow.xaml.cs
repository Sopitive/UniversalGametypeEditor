
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UniversalGametypeEditor.Properties;

namespace UniversalGametypeEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly Timer timer = new();
        static byte[] header = { 0x5F, 0x62, 0x6C, 0x66, 0x00, 0x00, 0x00, 0x30, 0x00, 0x01, 0x00, 0x02, 0xFF, 0xFE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x63, 0x68, 0x64, 0x72, 0x00, 0x00, 0x02, 0xC0, 0x00, 0x0A, 0x00, 0x02, 0xFF, 0xFF, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x29, 0x53, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x03, 0x02, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6D, 0x70, 0x76, 0x72, 0x00, 0x00, 0x50, 0x28, 0x00, 0x36, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20 };
        static byte[] ender = { 0x5F, 0x65, 0x6F, 0x66, 0x00, 0x00, 0x00, 0x11, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x53, 0x18, 0x00 };
        private readonly int reachLength = 20480;
        private readonly int h42aLength = 31744;
        private byte[] fileBytes = Array.Empty<byte>();
        public ObservableCollection<string> WatchedFilesList { get; set; } =
   new ObservableCollection<string>();


        public ObservableCollection<string> HotReloadFilesList { get; set; } =
   new ObservableCollection<string>();




        public MainWindow()
        {
            InitializeComponent();
            UpdateSettingsFromFile();

            FilesListWatched.SelectionChanged += FilesListWatched_SelectionChanged;
            this.DataContext = this;
            StateChanged += MainWindowStateChangeRaised;

            var menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            Action setAlignmentValue = () => {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            };
            setAlignmentValue();
            SystemParameters.StaticPropertyChanged += (sender, e) => { setAlignmentValue(); };

            if (Settings.Default.FilePath != "Undefined")
            {
                DirPath.Text = Settings.Default.FilePath;
                string folderName = Settings.Default.FilePath;
                GetFiles(folderName, WatchedFilesList);
                RegisterWatcher(folderName);
            }

            if (Settings.Default.HotReloadPath != "Undefined")
            {
                HotReloadDir.Text = Settings.Default.HotReloadPath;
                string folderName = Settings.Default.HotReloadPath;
                GetFiles(folderName, HotReloadFilesList);
            }
        }

        private void FilesListWatched_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleFiles((string)e.AddedItems[0], Settings.Default.FilePath, WatcherChangeTypes.Changed, false);
        }

        public void GetFiles(string dirName, ObservableCollection<string> collection)
        {
            string [] fileEntries = Directory.GetFiles(dirName);
            foreach (string filename in fileEntries)
            {
                collection.Add(Path.GetFileName(filename));
            }
        }

        public void UpdateSettingsFromFile()
        {
            ConvertBin.IsChecked = Settings.Default.ConvertBin;
        }

        private void UpdateLastEvent(string e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                LastEvent.Text = e
            ));
        }
        
        private void CheckConvertBin(object sender, RoutedEventArgs e)
        {
            Settings.Default.ConvertBin = ConvertBin.IsChecked;
            Settings.Default.Save();
        }

        public void HandleExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }


        public void HandleOpenClick(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        public void HandleSetHotReload(object sender, RoutedEventArgs e)
        {
            OpenFolder("Hot Reload");

        }


        public void OpenFile()
        {
            var filePath = String.Empty;
            var fileContent = String.Empty;
            OpenFileDialog openFile = new()
            {
                InitialDirectory = "C:\\",
                Filter = "txt files (*.txt)|binary files (*.bin)|megalo files (*.mglo)|*.txt|All Files (*.*)|(*.*)"
            };

            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePath = openFile.FileName;
                var fileStrem = openFile.OpenFile();
                using (StreamReader reader = new(fileStrem))
                {
                    fileContent = reader.ReadToEnd();
                }
            }
        }

        private void HandleSetDirClick(object sender, RoutedEventArgs e)
        {
            OpenFolder("File Path");
        }

        public void OpenFolder(string path)
        {
            string? folderName;
            UpdateLastEvent("Setting Directory");
            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                LastEvent.Text = "Set Directory";
                folderName = dialog.SelectedPath;

                if (path == "Hot Reload")
                {
                    Settings.Default.HotReloadPath = folderName;
                    HotReloadDir.Text = folderName;
                    Settings.Default.Save();
                }

                if (path == "File Path")
                {
                    Settings.Default.FilePath = folderName;
                    DirPath.Text = folderName;
                    Settings.Default.Save();
                }
                if (path == "File Path")
                {
                    RegisterWatcher(folderName);
                }
                
            }
        }
        private FileSystemWatcher watcher = new();
        public void RegisterWatcher(string foldername)
        {
            timer.Enabled = true;
            timer.Start();
            timer.Interval = 100;
            watcher.Path = foldername;

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += new FileSystemEventHandler(OnChange);
            watcher.Created += new FileSystemEventHandler(OnChange);

            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            UpdateLastEvent("Listening For File Changes...");
        }

        



        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Minimize
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        // Maximize
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        // Restore
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        // State change
        private void MainWindowStateChangeRaised(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                RestoreButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }

        
        public void HandleFiles(string name, string path, WatcherChangeTypes changeType, bool setDirectory)
        {
            string? directory;
            string? fullPath;

            if (changeType != WatcherChangeTypes.Changed)
            {
                UpdateLastEvent($"Created: {name}");
            }

            if (name.Contains(".bin") && !name.EndsWith(".bin"))
            {
                name = Regex.Replace(name, @"(.*)\..*", "$1");
                System.Threading.Thread.Sleep(100);
            }

            if (setDirectory)
            {
                fullPath = Path.GetDirectoryName(path) + "\\" + name;
                directory = Path.GetDirectoryName(path);
            } else
            {
                directory = path;
                fullPath = path + "\\" + name;
            }
                

            UpdateLastEvent($"Modified: {name}");

            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (Settings.Default.HotReloadPath == "Undefined")
            {
                UpdateLastEvent("Error: Hot Reload Path Not Set");
                return;
            }
            var copyPath = Settings.Default.HotReloadPath;

            if (name.EndsWith(".bin") && Settings.Default.ConvertBin)
            {
                ConvertToMglo(name, directory);
            }

            if (name.EndsWith(".bin") == false)
            {
                File.Copy($"{fullPath}", $"{copyPath}\\{name}", true);
                File.Copy($"{fullPath}", $"{copyPath}\\.mglo", true);
                UpdateLastEvent($"Copied: {name} to {copyPath}");
            }
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                UpdateHRListView(copyPath)
            ));
        }


        private void OnChange(object sender, FileSystemEventArgs e)
        {
            HandleFiles(e.Name, e.FullPath, e.ChangeType, true);

        }

        public void UpdateHRListView(string copyPath)
        {
            HotReloadFilesList.Clear();
            GetFiles(copyPath, HotReloadFilesList);
        }

        private void ConvertToMglo(string name, string directory)
        {
            //Convert a .bin file to a .mglo file here
            byte [] fileBytes = File.ReadAllBytes($"{directory}\\{name}");
            int length = 20480;
            byte[] newArray = new byte[length - 1];
            for (int i = 0; i < newArray.Length; i++)
            {
                newArray[i] = fileBytes[i + header.Length];
            }

            newArray = BitShift(length, newArray);
            File.WriteAllBytes($"{directory}\\{name.Replace(".bin","")}.mglo", newArray);
        }


        private byte[] BitShift(int length, byte[] array)
        {
            int previousUpperBits = 0;

            for (int i = 0; i<array.Length; i++)
            {
                var currentLowerBits = (array[i] << 4);
                array[i] = (byte)(array[i] >> 4);
                array[i] = array[i] |= (byte)previousUpperBits;
                previousUpperBits = currentLowerBits;
            }

            return array;
            
        }

        
    }
}
