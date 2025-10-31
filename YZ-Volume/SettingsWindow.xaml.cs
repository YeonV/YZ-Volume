using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using CheckBox = System.Windows.Controls.CheckBox;
using TextBox = System.Windows.Controls.TextBox;

namespace YZ_Volume
{
    public partial class SettingsWindow : Window
    {
        private List<Preset> _presets;
        private Dictionary<string, (CheckBox VisibiltyCheckBox, TextBox NameTextBox)> deviceControls = new();
        // NEW: A way to track the new textboxes
        private Dictionary<Preset, TextBox> _presetIndexTextBoxes = new();

        public SettingsWindow()
        {
            InitializeComponent();
            DwmApi.UseImmersiveDarkMode(this, true);

            LoadAndSeedPresets();
            LoadDevices();
            UpdatePresetManagerUI();

            VbanToggleButton.IsChecked = Properties.Settings.Default.VbanEnabled;
            VbanIpTextBox.Text = Properties.Settings.Default.VbanIpAddress;
            VbanPortTextBox.Text = Properties.Settings.Default.VbanPort.ToString();
            VbanToggleButton.Click += (s, e) => UpdateVbanTestPanelVisibility();
            UpdateVbanTestPanelVisibility();
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        private void LoadAndSeedPresets()
        {
            string json = Properties.Settings.Default.PresetsJson;
            if (!string.IsNullOrEmpty(json))
            {
                _presets = JsonConvert.DeserializeObject<List<Preset>>(json) ?? new List<Preset>();
            }

            if (_presets == null || _presets.Count == 0)
            {
                _presets = GetDefaultPresets();
            }
        }

        public List<Preset> GetDefaultPresets()
        {
            // Now we must include the VbanIndex for our defaults
            return new List<Preset> {
                new Preset {
                    Name = "PC 5.1", VbanIndex = 1,
                    Controls = new List<MatrixControl> {
                        new MatrixControl { Label = "FL", CommandBases = { "Point(VAIO2.IN[1],WIN1.OUT[1])" }, InitialGains = { -10.0 } },
                        new MatrixControl { Label = "FR", CommandBases = { "Point(VAIO2.IN[2],WIN1.OUT[2])" }, InitialGains = { -9.0 } },
                        new MatrixControl { Label = "C",  CommandBases = { "Point(VAIO2.IN[3],WIN3.OUT[1])", "Point(VAIO2.IN[3],WIN3.OUT[2])" }, InitialGains = { -6.0, -4.5 } },
                        new MatrixControl { Label = "S",  CommandBases = { "Point(VAIO2.IN[4],WIN1.OUT[1])", "Point(VAIO2.IN[4],WIN1.OUT[2])", "Point(VAIO2.IN[4],WIN4.OUT[1])", "Point(VAIO2.IN[4],WIN4.OUT[2])" }, InitialGains = { -10.0, -9.0, 0.0, -1.0 } },
                        new MatrixControl { Label = "RL", CommandBases = { "Point(VAIO2.IN[5],WIN4.OUT[1])" }, InitialGains = { 0.0 } },
                        new MatrixControl { Label = "RR", CommandBases = { "Point(VAIO2.IN[6],WIN4.OUT[2])" }, InitialGains = { -1.0 } }
                    }
                },
                new Preset {
                    Name = "PC 2.0", VbanIndex = 2,
                    Controls = new List<MatrixControl> {
                        new MatrixControl { Label = "FL", CommandBases = { "Point(VAIO2.IN[1],WIN1.OUT[1])", "Point(VAIO2.IN[1],WIN1.OUT[2])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "FR", CommandBases = { "Point(VAIO2.IN[2],WIN1.OUT[1])", "Point(VAIO2.IN[2],WIN1.OUT[2])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "C",  CommandBases = { "Point(VAIO2.IN[3],WIN1.OUT[1])", "Point(VAIO2.IN[3],WIN1.OUT[2])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "S",  CommandBases = { "Point(VAIO2.IN[4],WIN1.OUT[1])", "Point(VAIO2.IN[4],WIN1.OUT[2])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "RL", CommandBases = { "Point(VAIO2.IN[5],WIN1.OUT[1])", "Point(VAIO2.IN[5],WIN1.OUT[2])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "RR", CommandBases = { "Point(VAIO2.IN[6],WIN1.OUT[1])", "Point(VAIO2.IN[6],WIN1.OUT[2])" }, InitialGains = { 0.0, 0.0 } }
                    }
                },
                new Preset {
                    Name = "Beamer 5.1", VbanIndex = 3,
                    Controls = new List<MatrixControl> {
                        new MatrixControl { Label = "FL", CommandBases = { "Point(VAIO2.IN[1],WIN4.OUT[2])" }, InitialGains = { -1.0 } },
                        new MatrixControl { Label = "FR", CommandBases = { "Point(VAIO2.IN[2],WIN4.OUT[1])" }, InitialGains = { 0.0 } },
                        new MatrixControl { Label = "C",  CommandBases = { "Point(VAIO2.IN[3],WIN3.OUT[1])", "Point(VAIO2.IN[3],WIN3.OUT[2])", "Point(VAIO2.IN[3],WIN4.OUT[1])", "Point(VAIO2.IN[3],WIN4.OUT[2])" }, InitialGains = { -6.0, -4.5, -6.0, -7.0 } },
                        new MatrixControl { Label = "S",  CommandBases = { "Point(VAIO2.IN[4],WIN1.OUT[1])", "Point(VAIO2.IN[4],WIN1.OUT[2])", "Point(VAIO2.IN[4],WIN4.OUT[1])", "Point(VAIO2.IN[4],WIN4.OUT[2])" }, InitialGains = { -10.0, -9.0, 0.0, -1.0 } },
                        new MatrixControl { Label = "RL", CommandBases = { "Point(VAIO2.IN[5],WIN1.OUT[2])" }, InitialGains = { -9.0 } },
                        new MatrixControl { Label = "RR", CommandBases = { "Point(VAIO2.IN[6],WIN1.OUT[1])" }, InitialGains = { -10.0 } }
                    }
                }
            };
        }

        private void UpdatePresetManagerUI()
        {
            PresetManagerPanel.Children.Clear();
            _presetIndexTextBoxes.Clear(); // Clear the tracking dictionary

            foreach (var preset in _presets)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Index TextBox
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Export
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Delete

                var nameBlock = new TextBlock { Text = preset.Name, VerticalAlignment = VerticalAlignment.Center };

                // NEW: Create the TextBox for the VBAN Index
                var indexTextBox = new TextBox { Text = preset.VbanIndex.ToString(), Width = 40, Margin = new Thickness(5, 0, 5, 0) };
                _presetIndexTextBoxes[preset] = indexTextBox; // Track it

                var exportButton = new System.Windows.Controls.Button { Content = "\uE896", Style = (Style)FindResource("TestIconButtonStyle"), ToolTip = "Export to XML" };
                var deleteButton = new System.Windows.Controls.Button { Content = "\uE74D", Style = (Style)FindResource("TestIconButtonStyle"), ToolTip = "Delete", Margin = new Thickness(5, 0, 0, 0) };
                var currentPreset = preset;
                exportButton.Click += (s, e) => ExportPreset(currentPreset);
                deleteButton.Click += (s, e) => {
                    if (System.Windows.MessageBox.Show($"Are you sure you want to delete '{currentPreset.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        _presets.Remove(currentPreset);
                        UpdatePresetManagerUI();
                    }
                };

                Grid.SetColumn(nameBlock, 0);
                Grid.SetColumn(indexTextBox, 1); // Add to layout
                Grid.SetColumn(exportButton, 2);
                Grid.SetColumn(deleteButton, 3);

                grid.Children.Add(nameBlock);
                grid.Children.Add(indexTextBox); // Add to layout
                grid.Children.Add(exportButton);
                grid.Children.Add(deleteButton);
                PresetManagerPanel.Children.Add(grid);
            }
        }

        private void ImportPreset_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "XML Files (*.xml)|*.xml", Multiselect = true };
            if (openFileDialog.ShowDialog() != true) return;

            foreach (string filename in openFileDialog.FileNames)
            {
                try
                {
                    var newPreset = ParsePresetFromXml(filename);
                    // When importing, assign the next available index as a default
                    newPreset.VbanIndex = _presets.Count + 1;
                    _presets.Add(newPreset);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to import from '{Path.GetFileName(filename)}':\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            UpdatePresetManagerUI();
        }

        private Preset ParsePresetFromXml(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var preset = new Preset { Name = doc.Descendants("PresetName").FirstOrDefault()?.Value.Trim() ?? Path.GetFileNameWithoutExtension(filePath) };

            var inputMap = new Dictionary<string, string> { { "1", "FL" }, { "2", "FR" }, { "3", "C" }, { "4", "S" }, { "5", "RL" }, { "6", "RR" } };

            var groupedPoints = doc.Descendants("PresetPoint")
                .Where(p => p.Attribute("in") != null && inputMap.ContainsKey(p.Attribute("in").Value))
                .GroupBy(p => p.Attribute("in").Value);

            foreach (var group in groupedPoints.OrderBy(g => g.Key))
            {
                string inputNumber = group.Key;
                string label = inputMap[inputNumber];

                var commandBases = group.Select(p => $"Point({p.Attribute("slotin")?.Value}.IN[{p.Attribute("in")?.Value}],{p.Attribute("slotout")?.Value}[{p.Attribute("out")?.Value}])").ToList();
                var gains = group.Select(p => {
                    double.TryParse(p.Attribute("dBGain")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double gainVal);
                    return gainVal;
                }).ToList();

                preset.Controls.Add(new MatrixControl { Label = label, CommandBases = commandBases, InitialGains = gains });
            }
            return preset;
        }

        private void ExportPreset(Preset preset)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog { FileName = $"{preset.Name}.xml", Filter = "XML Files (*.xml)|*.xml" };
            if (saveFileDialog.ShowDialog() != true) return;
            var doc = new XDocument(new XElement("VBAudioMatrixPresetPatch"));
            int totalPoints = preset.Controls.Sum(c => c.CommandBases.Count);
            doc.Root.Add(new XElement("PresetName", new XAttribute("nbzone", "1"), new XAttribute("nbpoint", totalPoints), preset.Name));
            doc.Root.Add(new XElement("PresetComment"));
            doc.Root.Add(new XElement("PresetZone", new XAttribute("index", "0"), new XAttribute("slotin0", "WIN1.IN"), new XAttribute("in0", "1"), new XAttribute("slotout0", "WIN1.OUT"), new XAttribute("out0", "1"), new XAttribute("slotin1", "VAIO2.IN"), new XAttribute("in1", "8"), new XAttribute("slotout1", "VAIO2"), new XAttribute("out1", "8")));
            int index = 0;
            foreach (var control in preset.Controls)
            {
                for (int i = 0; i < control.CommandBases.Count; i++)
                {
                    var commandBase = control.CommandBases[i];
                    var gain = control.InitialGains[i];
                    var match = Regex.Match(commandBase, @"Point\((\w+\.IN)\[(\d+)\],(\w+\.OUT)\[(\d+)\]\)");
                    if (match.Success)
                    {
                        doc.Root.Add(new XElement("PresetPoint",
                            new XAttribute("index", index++),
                            new XAttribute("slotin", match.Groups[1].Value), new XAttribute("in", match.Groups[2].Value),
                            new XAttribute("slotout", match.Groups[3].Value), new XAttribute("out", match.Groups[4].Value),
                            new XAttribute("dBGain", gain.ToString("F2", CultureInfo.InvariantCulture)),
                            new XAttribute("mute", "0"), new XAttribute("phase", "0")
                        ));
                    }
                }
            }
            doc.Save(saveFileDialog.FileName);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.VbanEnabled = VbanToggleButton.IsChecked ?? false;
            Properties.Settings.Default.VbanIpAddress = VbanIpTextBox.Text;
            if (int.TryParse(VbanPortTextBox.Text, out int port)) Properties.Settings.Default.VbanPort = port;
            var visibleIDs = new System.Collections.Specialized.StringCollection();
            var customNamesDict = new Dictionary<string, string>();
            foreach (var pair in deviceControls)
            {
                if (pair.Value.VisibiltyCheckBox.IsChecked == true) visibleIDs.Add(pair.Key);
                if (!string.IsNullOrWhiteSpace(pair.Value.NameTextBox.Text)) customNamesDict.Add(pair.Key, pair.Value.NameTextBox.Text);
            }
            Properties.Settings.Default.VisibleDeviceIDs = visibleIDs;
            Properties.Settings.Default.CustomDeviceNames = JsonConvert.SerializeObject(customNamesDict);

            // --- NEW: Update preset objects with values from TextBoxes before saving ---
            foreach (var preset in _presets)
            {
                if (_presetIndexTextBoxes.TryGetValue(preset, out TextBox? indexBox))
                {
                    if (int.TryParse(indexBox.Text, out int newIndex))
                    {
                        preset.VbanIndex = newIndex;
                    }
                }
            }
            // --- END NEW ---

            Properties.Settings.Default.PresetsJson = JsonConvert.SerializeObject(_presets);
            Properties.Settings.Default.Save();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private void LoadDevices()
        {
            var savedVisibleIDs = Properties.Settings.Default.VisibleDeviceIDs;
            string? customNamesJson = Properties.Settings.Default.CustomDeviceNames;
            var customNamesDict = !string.IsNullOrEmpty(customNamesJson) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(customNamesJson) ?? new Dictionary<string, string>() : new Dictionary<string, string>();
            if (savedVisibleIDs == null) savedVisibleIDs = new System.Collections.Specialized.StringCollection();
            var enumerator = new MMDeviceEnumerator();
            var allDevices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            foreach (var device in allDevices)
            {
                var grid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var iconTextBlock = new System.Windows.Controls.TextBlock { FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"), Text = (device.DataFlow == DataFlow.Render) ? "\uE767" : "\uE720", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                var nameTextBlock = new System.Windows.Controls.TextBlock { Text = device.FriendlyName, VerticalAlignment = VerticalAlignment.Center };
                var contentPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                contentPanel.Children.Add(iconTextBlock);
                contentPanel.Children.Add(nameTextBlock);
                var checkBox = new System.Windows.Controls.CheckBox { Content = contentPanel, IsChecked = savedVisibleIDs.Contains(device.ID), VerticalAlignment = VerticalAlignment.Center };
                var textBox = new System.Windows.Controls.TextBox { Text = customNamesDict.ContainsKey(device.ID) ? customNamesDict[device.ID] : "", Margin = new Thickness(10, 0, 0, 0), Width = 150, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
                System.Windows.Controls.Grid.SetColumn(checkBox, 0);
                System.Windows.Controls.Grid.SetColumn(textBox, 1);
                grid.Children.Add(checkBox);
                grid.Children.Add(textBox);
                SettingsDeviceListPanel.Children.Add(grid);
                deviceControls.Add(device.ID, (checkBox, textBox));
            }
        }

        private void UpdateVbanTestPanelVisibility()
        {
            if (VbanTestPanel != null)
            {
                VbanTestPanel.Visibility = VbanToggleButton.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SendCustomCommand_Click(object sender, RoutedEventArgs e)
        {
            if (CustomCommandTextBox != null && !string.IsNullOrWhiteSpace(CustomCommandTextBox.Text))
            {
                SendVbanTestCommand(CustomCommandTextBox.Text);
            }
        }

        private void SendVbanTestCommand(string command)
        {
            string ipAddress = VbanIpTextBox.Text;
            if (!int.TryParse(VbanPortTextBox.Text, out int port))
            {
                System.Windows.MessageBox.Show("Invalid Port number.", "VBAN Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MatrixUdpClient? testClient = null;
            try
            {
                testClient = new MatrixUdpClient(ipAddress, port, "Command1");
                testClient.SendCommand(command);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to send VBAN command.\n\nError: {ex.Message}", "VBAN Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                testClient?.StopListener();
            }
        }
    }
}