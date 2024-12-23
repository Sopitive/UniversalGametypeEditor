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
using System.Media;
using Xceed.Wpf.Toolkit;
using Newtonsoft.Json;

namespace UniversalGametypeEditor
{
    public partial class PlaylistEditor : Window
    {
        private List<string> mapVariants;
        private List<string> mapVariantHashes;
        private List<string> gametypeHashes;
        private Dictionary<string, Dictionary<string, string>> gametypeMapVariantData;
        private List<GametypeItem> gametypeItems;
        private List<GametypeItem> filteredGametypeItems;

        public PlaylistEditor()
        {
            InitializeComponent();
            gametypeItems = new List<GametypeItem>(); // Initialize the list
            InitializeGametypeList();
            gametypeMapVariantData = new Dictionary<string, Dictionary<string, string>>();
            GametypeListBox.Loaded += GametypeListBox_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Display the FYI popup
            FyiPopup.IsOpen = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the FYI popup
            FyiPopup.IsOpen = false;
        }

        private void GametypeListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(GametypeListBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }


        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                // Show or hide the top scroll indicator
                TopIndicator.Visibility = scrollViewer.VerticalOffset > 0 ? Visibility.Visible : Visibility.Collapsed;

                // Show or hide the bottom scroll indicator
                BottomIndicator.Visibility = scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                {
                    return (T)child;
                }
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void InitializeGametypeList()
        {
            var (mapVariantTitles, mapVariantHashes, gametypeTitles, gametypeHashes, mapFolderNames, gametypeFolderNames) = CreatePlaylist.GetUUID();
            mapVariants = mapVariantTitles; // Store the map variants
            this.mapVariantHashes = mapVariantHashes; // Store the map variant hashes
            this.gametypeHashes = gametypeHashes; // Store the gametype hashes

            for (int i = 0; i < gametypeTitles.Count; i++)
            {
                string folderName = gametypeFolderNames[i];
                string gametypeName = gametypeTitles[i];
                var gametypeItem = new GametypeItem
                {
                    Name = $"{folderName}\\{gametypeName}",
                    Hash = gametypeHashes[i],
                    FolderName = folderName
                };
                gametypeItems.Add(gametypeItem);
            }

            // Initialize the filtered list with all items
            filteredGametypeItems = new List<GametypeItem>(gametypeItems);
            GametypeListBox.ItemsSource = null; // Clear the ItemsSource
            GametypeListBox.ItemsSource = filteredGametypeItems;
        }
        private void GametypeSearch_Focused(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Search Gametypes...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void GametypeSearch_Unfocused(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Search Gametypes...";
                SearchTextBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "Search Gametypes...")
            {
                return;
            }

            var searchText = SearchTextBox.Text.ToLower();
            filteredGametypeItems = gametypeItems
                .Where(item => item.Name.ToLower().Contains(searchText))
                .ToList();
            GametypeListBox.ItemsSource = null; // Clear the ItemsSource
            GametypeListBox.ItemsSource = filteredGametypeItems;
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
                // Remove the gametype DropDown from the MapVariantsPanel
                RemoveGametypeDropDown(gametypeItem);

                // Remove the gametype StackPanel from the PlaylistPanel
                var gametypeStackPanel = Playlist.Children
                    .OfType<StackPanel>()
                    .FirstOrDefault(sp => sp.Tag as string == gametypeItem.Name);

                if (gametypeStackPanel != null)
                {
                    Playlist.Children.Remove(gametypeStackPanel);
                }
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

            //var gametypeHashTextBlock = new TextBlock
            //{
            //    Text = $"Game Variant Hash: {gametypeItem.Hash}",
            //    Foreground = Brushes.White,
            //    Margin = new Thickness(0, 0, 0, 10)
            //};
            //gametypeDropDown.DropdownData.Children.Add(gametypeHashTextBlock);

            // Add the map search TextBox
            var mapSearchTextBox = new TextBox
            {
                Width = 200,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 10),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Tag = gametypeItem,
                Foreground = Brushes.Gray,
                Text = "Search Maps..."
            };
            mapSearchTextBox.GotFocus += MapSearchTextBox_GotFocus;
            mapSearchTextBox.LostFocus += MapSearchTextBox_LostFocus;
            mapSearchTextBox.TextChanged += MapSearchTextBox_TextChanged;
            gametypeDropDown.DropdownData.Children.Add(mapSearchTextBox);

            for (int i = 0; i < mapVariants.Count; i++)
            {
                var mapVariant = mapVariants[i];
                var mapVariantHash = mapVariantHashes[i];

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 5),
                    Tag = mapVariant // Use Tag to store the map variant name
                };
                var mapLabel = new TextBlock
                {
                    Text = mapVariant,
                    Width = 200,
                    Foreground = Brushes.White
                };

                var numericUpDown = new IntegerUpDown
                {
                    Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)),
                    Foreground = Brushes.White,
                    Width = 50,
                    Value = 0,
                    Minimum = 0,
                    Tag = mapVariant, // Use Tag to store the map variant name
                    DataContext = gametypeItem // Set DataContext to the gametype item
                };

                numericUpDown.ValueChanged += NumericUpDown_ValueChanged;

                stackPanel.Children.Add(mapLabel);
                stackPanel.Children.Add(numericUpDown);
                gametypeDropDown.DropdownData.Children.Add(stackPanel);

                //var hashTextBlock = new TextBlock
                //{
                //    Text = $"Hash: {mapVariantHash}",
                //    Foreground = Brushes.White,
                //    Margin = new Thickness(0, 0, 0, 10)
                //};
                //gametypeDropDown.DropdownData.Children.Add(hashTextBlock);
            }

            // Add the new DropDown instance to the MapVariantsPanel
            MapVariantsPanel.Children.Add(gametypeDropDown);
        }

        private void MapSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "Search Maps...")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void MapSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Search Maps...";
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void MapSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var gametypeItem = textBox.Tag as GametypeItem;
            var searchText = textBox.Text.ToLower();

            if (textBox.Text == "Search Maps...")
            {
                return;
            }

            var gametypeDropDown = MapVariantsPanel.Children
                .OfType<DropDown>()
                .FirstOrDefault(dd => dd.Tag == gametypeItem);

            if (gametypeDropDown != null)
            {
                foreach (var child in gametypeDropDown.DropdownData.Children)
                {
                    if (child is StackPanel stackPanel && stackPanel.Tag is string mapVariant)
                    {
                        stackPanel.Visibility = mapVariant.ToLower().Contains(searchText) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
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
                    if (child is StackPanel stackPanel && stackPanel.Children[1] is IntegerUpDown numericUpDown)
                    {
                        var mapVariant = numericUpDown.Tag as string;
                        if (mapVariantData.TryGetValue(mapVariant, out var value))
                        {
                            numericUpDown.Value = int.Parse(value);
                        }
                        else
                        {
                            numericUpDown.Value = 0;
                        }
                    }
                }
            }
            else
            {
                foreach (var child in MapVariantsExpander.DropdownData.Children)
                {
                    if (child is StackPanel stackPanel && stackPanel.Children[1] is IntegerUpDown numericUpDown)
                    {
                        numericUpDown.Value = 0;
                    }
                }
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is IntegerUpDown numericUpDown)
            {
                var mapVariant = numericUpDown.Tag as string;
                var gametypeItem = numericUpDown.DataContext as GametypeItem;

                // Find the parent StackPanel
                var stackPanel = numericUpDown.Parent as StackPanel;
                if (stackPanel != null)
                {
                    // Find the associated TextBlock (map label)
                    var mapLabel = stackPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Text == mapVariant);
                    if (mapLabel != null)
                    {
                        // Update the color based on the value
                        mapLabel.Foreground = numericUpDown.Value > 0 ? Brushes.LimeGreen : Brushes.White;
                    }
                }

                if (gametypeItem != null)
                {
                    // Find the gametype StackPanel
                    var gametypeStackPanel = Playlist.Children
                        .OfType<StackPanel>()
                        .FirstOrDefault(sp => sp.Tag as string == gametypeItem.Name);

                    if (numericUpDown.Value.HasValue && numericUpDown.Value.Value != 0)
                    {
                        if (gametypeStackPanel == null)
                        {
                            gametypeStackPanel = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                Tag = gametypeItem.Name,
                                Margin = new Thickness(0, 10, 0, 0)
                            };

                            var gametypeTextBlock = new TextBlock
                            {
                                Text = gametypeItem.Name,
                                FontWeight = FontWeights.Bold,
                                Foreground = Brushes.White,
                                FontSize = 16
                            };
                            gametypeStackPanel.Children.Add(gametypeTextBlock);
                            Playlist.Children.Add(gametypeStackPanel);
                        }

                        // Find or create the map TextBlock
                        var mapTextBlock = gametypeStackPanel.Children
                            .OfType<TextBlock>()
                            .FirstOrDefault(tb => tb.Tag as string == mapVariant);

                        if (mapTextBlock == null)
                        {
                            mapTextBlock = new TextBlock
                            {
                                Text = $"{mapVariant} x{numericUpDown.Value}",
                                Foreground = Brushes.White,
                                Margin = new Thickness(20, 0, 0, 0),
                                Tag = mapVariant,
                                FontSize = 16
                            };
                            gametypeStackPanel.Children.Add(mapTextBlock);
                        }
                        else
                        {
                            // Update the text to reflect the new count
                            mapTextBlock.Text = $"{mapVariant} x{numericUpDown.Value}";
                        }
                    }
                    else
                    {
                        // Remove the map TextBlock if the value is 0
                        if (gametypeStackPanel != null)
                        {
                            var mapTextBlock = gametypeStackPanel.Children
                                .OfType<TextBlock>()
                                .FirstOrDefault(tb => tb.Tag as string == mapVariant);

                            if (mapTextBlock != null)
                            {
                                gametypeStackPanel.Children.Remove(mapTextBlock);
                            }

                            // Remove the gametype StackPanel if it has no maps
                            if (gametypeStackPanel.Children.Count == 1)
                            {
                                Playlist.Children.Remove(gametypeStackPanel);
                            }
                        }
                    }
                }
            }
        }



        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the MapVariantsPanel has any items
            if (!MapVariantsPanel.Children.OfType<DropDown>().Any())
            {
                return; // Exit the method if there are no items
            }
            SystemSounds.Beep.Play();
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
                                        var numericUpDown = stackPanel.Children.OfType<IntegerUpDown>().FirstOrDefault();
                                        if (numericUpDown != null && numericUpDown.Tag as string == mapVariant && numericUpDown.Value.HasValue && numericUpDown.Value.Value > 0)
                                        {
                                            for (int j = 0; j < numericUpDown.Value.Value; j++)
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


        private void SavePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SavePlaylist(saveFileDialog.FileName);
            }
        }

        public List<PlaylistItem> LoadPlaylist(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<PlaylistItem>>(json);
        }

        private void LoadPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadPlaylistData(openFileDialog.FileName);
            }
        }

        private void ResetUIElements()
        {
            foreach (var gametypeItem in gametypeItems)
            {
                gametypeItem.IsChecked = false;
            }
            MapVariantsPanel.Children.Clear();
        }

        private void LoadPlaylistData(string filePath)
        {
            ResetUIElements();
            var playlistItems = LoadPlaylist(filePath);

            foreach (var playlistItem in playlistItems)
            {
                // Find the matching gametype item
                var gametypeItem = gametypeItems.FirstOrDefault(g => g.Hash == playlistItem.GametypeHash);
                if (gametypeItem != null)
                {
                    // Check the checkbox for the gametype item
                    gametypeItem.IsChecked = true;

                    // Create the gametype dropdown
                    AddGametypeDropDown(gametypeItem);

                    // Find the corresponding DropDown in the MapVariantsPanel
                    var gametypeDropDown = MapVariantsPanel.Children
                        .OfType<DropDown>()
                        .FirstOrDefault(dd => dd.Tag == gametypeItem);

                    if (gametypeDropDown != null)
                    {
                        foreach (var mapItem in playlistItem.Maps)
                        {
                            // Find the matching map variant
                            var mapIndex = mapVariants.IndexOf(mapItem.MapName);
                            if (mapIndex != -1 && mapVariantHashes[mapIndex] == mapItem.MapHash)
                            {
                                // Find the corresponding IntegerUpDown control
                                var stackPanel = gametypeDropDown.DropdownData.Children
                                    .OfType<StackPanel>()
                                    .FirstOrDefault(sp => (sp.Children[0] as TextBlock)?.Text == mapItem.MapName);

                                if (stackPanel != null)
                                {
                                    var numericUpDown = stackPanel.Children[1] as IntegerUpDown;
                                    if (numericUpDown != null)
                                    {
                                        numericUpDown.Value = mapItem.Count;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // Refresh the ListBox
            GametypeListBox.ItemsSource = null;
            GametypeListBox.Items.Clear();
            GametypeListBox.ItemsSource = gametypeItems;
        }


        private void SavePlaylist(string filePath)
        {
            // Check if the MapVariantsPanel has any items
            if (!MapVariantsPanel.Children.OfType<DropDown>().Any())
            {
                return; // Exit the method if there are no items
            }

            var playlistItems = new List<PlaylistItem>();

            foreach (var gametypeDropDown in MapVariantsPanel.Children.OfType<DropDown>())
            {
                var gametypeItem = gametypeDropDown.Tag as GametypeItem;
                if (gametypeItem != null)
                {
                    var maps = new List<MapItem>();

                    foreach (var stackPanel in gametypeDropDown.DropdownData.Children.OfType<StackPanel>())
                    {
                        var mapLabel = stackPanel.Children[0] as TextBlock;
                        var numericUpDown = stackPanel.Children[1] as IntegerUpDown;

                        if (mapLabel != null && numericUpDown != null && numericUpDown.Value.HasValue && numericUpDown.Value.Value > 0)
                        {
                            var mapItem = new MapItem
                            {
                                MapName = mapLabel.Text,
                                MapHash = mapVariantHashes[mapVariants.IndexOf(mapLabel.Text)],
                                Count = numericUpDown.Value.Value
                            };
                            maps.Add(mapItem);
                        }
                    }

                    if (maps.Any())
                    {
                        var playlistItem = new PlaylistItem
                        {
                            GametypeName = gametypeItem.Name,
                            GametypeHash = gametypeItem.Hash,
                            Maps = maps
                        };
                        playlistItems.Add(playlistItem);
                    }
                }
            }

            var json = JsonConvert.SerializeObject(playlistItems, Formatting.Indented);
            File.WriteAllText(filePath, json);
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
            if (this.WindowState == System.Windows.WindowState.Maximized)
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
        public string Hash { get; set; }
        public string FolderName { get; set; } // Add this property
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

    public class PlaylistItem
    {
        public string GametypeName { get; set; }
        public string GametypeHash { get; set; }
        public List<MapItem> Maps { get; set; }
    }

    public class MapItem
    {
        public string MapName { get; set; }
        public string MapHash { get; set; }
        public int Count { get; set; }
    }



}
