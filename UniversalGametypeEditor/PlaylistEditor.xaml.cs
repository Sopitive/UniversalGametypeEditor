using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json.Serialization;
using System.IO;

namespace UniversalGametypeEditor
{
    public partial class PlaylistEditor : Window
    {
        private List<string> mapVariants;
        private List<string> mapVariantHashes;
        private List<string> gametypeHashes;
        private Dictionary<string, Dictionary<string, string>> gametypeMapVariantData;

        public PlaylistEditor()
        {
            InitializeComponent();
            InitializeGametypeList();
            gametypeMapVariantData = new Dictionary<string, Dictionary<string, string>>();
        }

        private void InitializeGametypeList()
        {
            var (mapVariantTitles, mapVariantHashes, gametypeTitles, gametypeHashes) = CreatePlaylist.GetUUID();
            mapVariants = mapVariantTitles; // Store the map variants
            this.mapVariantHashes = mapVariantHashes; // Store the map variant hashes
            this.gametypeHashes = gametypeHashes; // Store the gametype hashes

            for (int i = 0; i < gametypeTitles.Count; i++)
            {
                GametypeListBox.Items.Add(new GametypeItem { Name = gametypeTitles[i], Hash = gametypeHashes[i] });
            }
        }



        private void GametypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var gametypeItem = checkBox.DataContext as GametypeItem;

            if (gametypeItem != null)
            {
                AddGametypeDropDown(gametypeItem);
            }
        }

        private void GametypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var gametypeItem = checkBox.DataContext as GametypeItem;

            if (gametypeItem != null)
            {
                RemoveGametypeDropDown(gametypeItem);
            }
        }

        private void AddGametypeDropDown(GametypeItem gametypeItem)
        {
            // Create a new instance of the DropDown class for the gametype
            var gametypeDropDown = new DropDown
            {
                Margin = new Thickness(0, 5, 0, 5),
                Tag = gametypeItem // Use Tag to store the gametype item
            };

            gametypeDropDown.Expander.Header = gametypeItem.Name;
            gametypeDropDown.Scroller.MaxHeight = 500;

            var gametypeHashTextBlock = new TextBlock { Text = $"Game Variant Hash: {gametypeItem.Hash}", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) };
            gametypeDropDown.DropdownData.Children.Add(gametypeHashTextBlock);

            for (int i = 0; i < mapVariants.Count; i++)
            {
                var mapVariant = mapVariants[i];
                var mapVariantHash = mapVariantHashes[i];

                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                var mapLabel = new TextBlock { Text = mapVariant, Width = 200, Foreground = Brushes.White };
                var numericUpDown = new TextBox
                {
                    Width = 50,
                    Text = "0",
                    Style = (Style)FindResource("WhiteTextBoxStyle"),
                    Tag = mapVariant // Use Tag to store the map variant name
                };

                stackPanel.Children.Add(mapLabel);
                stackPanel.Children.Add(numericUpDown);
                gametypeDropDown.DropdownData.Children.Add(stackPanel);

                var hashTextBlock = new TextBlock { Text = $"Hash: {mapVariantHash}", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) };
                gametypeDropDown.DropdownData.Children.Add(hashTextBlock);
            }

            // Add the new DropDown instance to the MapVariantsPanel
            MapVariantsPanel.Children.Add(gametypeDropDown);
        }

        private void RemoveGametypeDropDown(GametypeItem gametypeItem)
        {
            // Find and remove the DropDown instance associated with the gametype item
            var dropDownToRemove = MapVariantsPanel.Children
                .OfType<DropDown>()
                .FirstOrDefault(d => d.Tag == gametypeItem);

            if (dropDownToRemove != null)
            {
                MapVariantsPanel.Children.Remove(dropDownToRemove);
            }
        }

        private string GetGametypeHash(string gametype)
        {
            if (gametypeMapVariantData.TryGetValue(gametype, out var mapVariantData) && mapVariantData.TryGetValue("Hash", out var gametypeHash))
            {
                return gametypeHash;
            }
            return null;
        }

        private void GametypeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GametypeListBox.SelectedItem is GametypeItem selectedGametype)
            {
                UpdateMapVariantsPanel(selectedGametype.Name);
            }
        }

        private void UpdateMapVariantsPanel(string gametype)
        {
            if (gametypeMapVariantData.TryGetValue(gametype, out var mapVariantData))
            {
                foreach (var child in MapVariantsExpander.DropdownData.Children)
                {
                    if (child is StackPanel stackPanel && stackPanel.Children[1] is TextBox textBox)
                    {
                        var mapVariant = textBox.Tag as string;
                        if (mapVariantData.TryGetValue(mapVariant, out var value))
                        {
                            textBox.Text = value;
                        }
                        else
                        {
                            textBox.Text = "0";
                        }
                    }
                }
            }
            else
            {
                foreach (var child in MapVariantsExpander.DropdownData.Children)
                {
                    if (child is StackPanel stackPanel && stackPanel.Children[1] is TextBox textBox)
                    {
                        textBox.Text = "0";
                    }
                }
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var jsonRoot = new Dictionary<string, object>
    {
        { "Version", 1 }
    };

            int variantIndex = 0;

            foreach (var item in GametypeListBox.Items)
            {
                if (item is GametypeItem gametypeItem && gametypeItem.IsChecked)
                {
                    var variantDict = new Dictionary<string, object>
            {
                { "VariantId", GenerateMD5Hash(Guid.NewGuid().ToString()) },
                { "Name", gametypeItem.Name },
                { "LibraryId", "reach" },
                { "GameVariantID", gametypeItem.Hash }
            };

                    int mapIndex = 0;
                    foreach (var child in MapVariantsPanel.Children)
                    {
                        if (child is DropDown dropDown && dropDown.Tag is GametypeItem dropDownGametypeItem && dropDownGametypeItem.Name == gametypeItem.Name)
                        {
                            for (int i = 0; i < mapVariants.Count; i++)
                            {
                                var mapVariant = mapVariants[i];
                                var mapVariantHash = mapVariantHashes[i];

                                foreach (var mapChild in dropDown.DropdownData.Children)
                                {
                                    if (mapChild is StackPanel stackPanel)
                                    {
                                        var textBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault();
                                        if (textBox != null && textBox.Tag as string == mapVariant && int.TryParse(textBox.Text, out int count) && count > 0)
                                        {
                                            for (int j = 0; j < count; j++)
                                            {
                                                variantDict[$"Maps[{mapIndex}]"] = new { MapVariantID = mapVariantHash };
                                                mapIndex++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Add Tag and Mode after Maps
                    variantDict["Tag"] = 0;
                    variantDict["Mode"] = 3;

                    jsonRoot[$"Variants[{variantIndex}]"] = variantDict;
                    variantIndex++;
                }
            }

            // Serialize to JSON
            var jsonString = System.Text.Json.JsonSerializer.Serialize(jsonRoot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            // Define the path to the LocalFiles directory in LocalLow
            string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\MCC\LocalFiles");

            // Get the list of directories in the LocalFiles folder
            var directories = Directory.GetDirectories(localLowPath);

            // Overwrite PlaylistVariants.json in each directory
            foreach (var directory in directories)
            {
                string filePath = Path.Combine(directory, "PlaylistVariants.json");
                File.WriteAllText(filePath, jsonString);
            }
        }









        private string GenerateMD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return new Guid(hashBytes).ToString();
            }
        }


        #region WindowCommands
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

        //Close all other windows when this window is closed
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
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
        #endregion
    }

    public class GametypeItem
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public string Hash { get; set; } // Add this property
    }

    

public class Variant
    {
        public string VariantId { get; set; }
        public string Name { get; set; }
        public string LibraryId { get; set; }
        public string GameVariantID { get; set; }
        public List<Map> Maps { get; set; }
        public int Tag { get; set; }
        public int Mode { get; set; }
    }

    public class Map
    {
        public string MapVariantID { get; set; }
    }

    public class JsonRoot
    {
        public int Version { get; set; }
        public Dictionary<string, object> Variants { get; set; }
    }


}
