using AudioSwitcher.AudioApi.CoreAudio;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace YZ_Volume
{
    public partial class SettingsWindow : Window
    {
        private List<Preset> _presets = new();
        private Dictionary<string, (System.Windows.Controls.CheckBox VisibiltyCheckBox, System.Windows.Controls.TextBox NameTextBox)> deviceControls = new();
        private Dictionary<Preset, System.Windows.Controls.TextBox> _presetIndexTextBoxes = new();
        private MatrixUdpClient? _consoleVbanClient;
        private List<string> _sentHistory = new();
        private int _historyIndex = -1;
        private bool _isWaitingForConfigFile = false;
        private bool _isConsoleSilent = false;

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
            VbanToggleButton.Click += (s, e) => {
                UpdateVbanTestPanelVisibility();
                if (VbanToggleButton.IsChecked == true) StartConsoleListener();
                else StopConsoleListener();
            };
            UpdateVbanTestPanelVisibility();

            PopulatePlaybackDevices();
            ShowConsoleToggleButton.Click += (s, e) => UpdateVbanTestPanelVisibility();
            AutoSelectDeviceCheckBox.IsChecked = Properties.Settings.Default.AutoSelectDeviceEnabled;
            string savedDeviceId = Properties.Settings.Default.DefaultDeviceId;
            foreach (ComboBoxItem item in DefaultDeviceComboBox.Items) {
                if (item.Tag?.ToString() == savedDeviceId) {
                    DefaultDeviceComboBox.SelectedItem = item;
                    break;
                }
            }
            UpdateDeviceComboBoxVisibility();
            if (VbanToggleButton.IsChecked == true) {
                StartConsoleListener();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            StopConsoleListener();
            base.OnClosing(e);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        
        private void LoadAndSeedPresets()
        {
            string json = Properties.Settings.Default.PresetsJson;
            if (!string.IsNullOrEmpty(json)) {
                _presets = JsonConvert.DeserializeObject<List<Preset>>(json) ?? new List<Preset>();
            }
            if (_presets == null || _presets.Count == 0) {
                _presets = GetDefaultPresets();
            }
        }

        public static List<Preset> GetDefaultPresets()
        {
            return new List<Preset> {
                new Preset { Name = "PC 5.1", VbanIndex = 1, Controls = new List<MatrixControl> {
                        new MatrixControl { Label = "FL", CommandBases = { "Point(VAIO2.IN[1],WIN1.OUT[1])" }, InitialGains = { -10.0 } },
                        new MatrixControl { Label = "FR", CommandBases = { "Point(VAIO2.IN[2],WIN1.OUT[2])" }, InitialGains = { -9.0 } },
                        new MatrixControl { Label = "C",  CommandBases = { "Point(VAIO2.IN[3],WIN3.OUT[1])", "Point(VAIO2.IN[3],WIN3.OUT[2])" }, InitialGains = { -6.0, -4.5 } },
                        new MatrixControl { Label = "S",  CommandBases = { "Point(VAIO2.IN[4],WIN1.OUT[1])", "Point(VAIO2.IN[4],WIN1.OUT[2])", "Point(VAIO2.IN[4],WIN4.OUT[1])", "Point(VAIO2.IN[4],WIN4.OUT[2])" }, InitialGains = { -10.0, -9.0, 0.0, -1.0 } },
                        new MatrixControl { Label = "RL", CommandBases = { "Point(VAIO2.IN[5],WIN4.OUT[1])" }, InitialGains = { 0.0 } },
                        new MatrixControl { Label = "RR", CommandBases = { "Point(VAIO2.IN[6],WIN4.OUT[2])" }, InitialGains = { -1.0 } }
                }},
                new Preset { Name = "PC 2.0", VbanIndex = 2, Controls = new List<MatrixControl> {
                        new MatrixControl { Label = "FL", CommandBases = { "Point(VAIO2.IN[1],WIN1.OUT[1])", "Point(VAIO2.IN[1],WIN4.OUT[1])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "FR", CommandBases = { "Point(VAIO2.IN[2],WIN1.OUT[2])", "Point(VAIO2.IN[2],WIN4.OUT[2])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "C",  CommandBases = { "Point(VAIO2.IN[3],WIN1.OUT[1])", "Point(VAIO2.IN[3],WIN1.OUT[2])", "Point(VAIO2.IN[3],WIN4.OUT[1])", "Point(VAIO2.IN[3],WIN4.OUT[2])" }, InitialGains = { 0.0, 0.0, 0.0, 0.0 } },
                        new MatrixControl { Label = "S",  CommandBases = { "Point(VAIO2.IN[4],WIN1.OUT[1])", "Point(VAIO2.IN[4],WIN1.OUT[2])", "Point(VAIO2.IN[4],WIN4.OUT[1])", "Point(VAIO2.IN[4],WIN4.OUT[2])" }, InitialGains = { 0.0, 0.0, 0.0, 0.0 } },
                        new MatrixControl { Label = "RL", CommandBases = { "Point(VAIO2.IN[5],WIN1.OUT[1])", "Point(VAIO2.IN[5],WIN4.OUT[1])" }, InitialGains = { 0.0, 0.0 } },
                        new MatrixControl { Label = "RR", CommandBases = { "Point(VAIO2.IN[6],WIN1.OUT[2])", "Point(VAIO2.IN[6],WIN4.OUT[2])" }, InitialGains = { 0.0, 0.0 } }
                }},
                new Preset { Name = "Beamer 5.1", VbanIndex = 3, Controls = new List<MatrixControl> {
                        new MatrixControl { Label = "FL", CommandBases = { "Point(VAIO2.IN[1],WIN4.OUT[2])" }, InitialGains = { -1.0 } },
                        new MatrixControl { Label = "FR", CommandBases = { "Point(VAIO2.IN[2],WIN4.OUT[1])" }, InitialGains = { 0.0 } },
                        new MatrixControl { Label = "C",  CommandBases = { "Point(VAIO2.IN[3],WIN3.OUT[1])", "Point(VAIO2.IN[3],WIN3.OUT[2])", "Point(VAIO2.IN[3],WIN4.OUT[1])", "Point(VAIO2.IN[3],WIN4.OUT[2])" }, InitialGains = { -6.0, -4.5, -6.0, -7.0 } },
                        new MatrixControl { Label = "S",  CommandBases = { "Point(VAIO2.IN[4],WIN1.OUT[1])", "Point(VAIO2.IN[4],WIN1.OUT[2])", "Point(VAIO2.IN[4],WIN4.OUT[1])", "Point(VAIO2.IN[4],WIN4.OUT[2])" }, InitialGains = { -10.0, -9.0, 0.0, -1.0 } },
                        new MatrixControl { Label = "RL", CommandBases = { "Point(VAIO2.IN[5],WIN1.OUT[2])" }, InitialGains = { -9.0 } },
                        new MatrixControl { Label = "RR", CommandBases = { "Point(VAIO2.IN[6],WIN1.OUT[1])" }, InitialGains = { -10.0 } }
                }}
            };
        }

        private void GetLiveState_Click(object sender, RoutedEventArgs e)
        {
            if (_consoleVbanClient == null)
            {
                System.Windows.MessageBox.Show("VBAN is not active.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddToHistory("--- Importing from Live State: Requesting config file path... ---", "SYSTEM");
            _isWaitingForConfigFile = true; // Set the flag
            _consoleVbanClient.SendCommand("Command.Load = ?");
        }

        private void UpdatePresetManagerUI()
        {
            PresetManagerPanel.Children.Clear();
            _presetIndexTextBoxes.Clear();
            foreach (var preset in _presets) {
                var grid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 5) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var nameBlock = new System.Windows.Controls.TextBlock { Text = preset.Name, VerticalAlignment = VerticalAlignment.Center };
                var indexTextBox = new System.Windows.Controls.TextBox { Text = preset.VbanIndex.ToString(), Width = 40, Margin = new Thickness(5, 0, 5, 0) };
                _presetIndexTextBoxes[preset] = indexTextBox;
                var exportButton = new System.Windows.Controls.Button { Content = "\uE896", Style = (Style)FindResource("TestIconButtonStyle"), Width = 50, ToolTip = "Export to XML" };
                var deleteButton = new System.Windows.Controls.Button { Content = "\uE74D", Style = (Style)FindResource("TestIconButtonStyle"), Width = 50, ToolTip = "Delete", Margin = new Thickness(5, 0, 0, 0) };
                var currentPreset = preset;
                exportButton.Click += (s, e) => ExportPreset(currentPreset);
                deleteButton.Click += (s, e) => {
                    if (System.Windows.MessageBox.Show($"Are you sure you want to delete '{currentPreset.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                        _presets.Remove(currentPreset);
                        UpdatePresetManagerUI();
                    }
                };
                System.Windows.Controls.Grid.SetColumn(nameBlock, 0);
                System.Windows.Controls.Grid.SetColumn(indexTextBox, 1);
                System.Windows.Controls.Grid.SetColumn(exportButton, 2);
                System.Windows.Controls.Grid.SetColumn(deleteButton, 3);
                grid.Children.Add(nameBlock);
                grid.Children.Add(indexTextBox);
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
                    newPreset.VbanIndex = _presets.Count + 1;
                    _presets.Add(newPreset);
                }
                catch (Exception ex) { System.Windows.MessageBox.Show($"Failed to import '{Path.GetFileName(filename)}':\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            UpdatePresetManagerUI();
        }

        private Preset ParsePresetFromXml(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            if (doc.Root == null) throw new InvalidDataException("XML file is empty or invalid.");
            var preset = new Preset { Name = doc.Descendants("PresetName").FirstOrDefault()?.Value.Trim() ?? Path.GetFileNameWithoutExtension(filePath) };
            var inputMap = new Dictionary<string, string> { { "1", "FL" }, { "2", "FR" }, { "3", "C" }, { "4", "S" }, { "5", "RL" }, { "6", "RR" } };
            var groupedPoints = doc.Descendants("PresetPoint")
                .Where(p => { var inAttribute = p.Attribute("in"); return inAttribute != null && !string.IsNullOrEmpty(inAttribute.Value) && inputMap.ContainsKey(inAttribute.Value); })
                .GroupBy(p => p.Attribute("in")!.Value);
            foreach (var group in groupedPoints.OrderBy(g => g.Key))
            {
                string inputNumber = group.Key;
                string label = inputMap[inputNumber];
                var commandBases = group.Select(p => $"Point({p.Attribute("slotin")?.Value}.IN[{p.Attribute("in")?.Value}],{p.Attribute("slotout")?.Value}[{p.Attribute("out")?.Value}])").ToList();
                var gains = group.Select(p => { double.TryParse(p.Attribute("dBGain")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double gainVal); return gainVal; }).ToList();
                preset.Controls.Add(new MatrixControl { Label = label, CommandBases = commandBases, InitialGains = gains });
            }
            return preset;
        }

        private void ExportPreset(Preset preset)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog { FileName = $"{preset.Name}.xml", Filter = "XML Files (*.xml)|*.xml" };
            if (saveFileDialog.ShowDialog() != true) return;
            var doc = new XDocument(new XElement("VBAudioMatrixPresetPatch"));
            if (doc.Root == null) return;
            int totalPoints = preset.Controls.Sum(c => c.CommandBases.Count);
            doc.Root.Add(new XElement("PresetName", new XAttribute("nbzone", "1"), new XAttribute("nbpoint", totalPoints), preset.Name));
            doc.Root.Add(new XElement("PresetComment"));
            doc.Root.Add(new XElement("PresetZone", new XAttribute("index", "0")));
            int index = 0;
            foreach (var control in preset.Controls)
            {
                for (int i = 0; i < control.CommandBases.Count; i++)
                {
                    var commandBase = control.CommandBases[i];
                    var gain = control.InitialGains[i];
                    var match = Regex.Match(commandBase, @"Point\((\w+)\.IN\[(\d+)\],(\w+)\.OUT\[(\d+)\]\)");
                    if (match.Success)
                    {
                        doc.Root.Add(new XElement("PresetPoint",
                            new XAttribute("index", index++), new XAttribute("slotin", match.Groups[1].Value),
                            new XAttribute("in", match.Groups[2].Value), new XAttribute("slotout", match.Groups[3].Value),
                            new XAttribute("out", match.Groups[4].Value), new XAttribute("dBGain", gain.ToString("F2", CultureInfo.InvariantCulture)),
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
            foreach (var pair in deviceControls) {
                if (pair.Value.VisibiltyCheckBox.IsChecked == true) visibleIDs.Add(pair.Key);
                if (!string.IsNullOrWhiteSpace(pair.Value.NameTextBox.Text)) customNamesDict.Add(pair.Key, pair.Value.NameTextBox.Text);
            }
            Properties.Settings.Default.VisibleDeviceIDs = visibleIDs;
            Properties.Settings.Default.CustomDeviceNames = JsonConvert.SerializeObject(customNamesDict);
            foreach (var preset in _presets) {
                if (_presetIndexTextBoxes.TryGetValue(preset, out var indexBox)) {
                    if (int.TryParse(indexBox.Text, out int newIndex)) {
                        preset.VbanIndex = newIndex;
                    }
                }
            }
            Properties.Settings.Default.AutoSelectDeviceEnabled = AutoSelectDeviceCheckBox.IsChecked ?? false;
            if (DefaultDeviceComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null) {
                Properties.Settings.Default.DefaultDeviceId = selectedItem.Tag.ToString();
            } else {
                Properties.Settings.Default.DefaultDeviceId = string.Empty;
            }
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
            foreach (var device in allDevices) {
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
            bool isVbanEnabled = VbanToggleButton.IsChecked == true;
            bool isConsoleVisible = ShowConsoleToggleButton.IsChecked == true;

            // Show the Sync button only if VBAN is enabled
            SyncButton.Visibility = isVbanEnabled ? Visibility.Visible : Visibility.Collapsed;

            // Show the ShowConsole button only if VBAN is enabled
            ShowConsoleToggleButton.Visibility = isVbanEnabled ? Visibility.Visible : Visibility.Collapsed;

            // Show the Test Panel only if BOTH are enabled
            if (VbanTestPanel != null)
            {
                VbanTestPanel.Visibility = (isVbanEnabled && isConsoleVisible) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void StartConsoleListener()
        {
            if (_consoleVbanClient != null) StopConsoleListener();
            if (int.TryParse(VbanPortTextBox.Text, out int port)) {
                _consoleVbanClient = new MatrixUdpClient(VbanIpTextBox.Text, port, "Command1");
                _consoleVbanClient.OnTextReplyReceived += OnVbanReply;
                _consoleVbanClient.StartListener();
                AddToHistory("--- Console Listener Started ---", "SYSTEM");
            }
        }

        private void StopConsoleListener()
        {
            if (_consoleVbanClient != null) {
                _consoleVbanClient.OnTextReplyReceived -= OnVbanReply;
                _consoleVbanClient.StopListener();
                _consoleVbanClient = null;
                AddToHistory("--- Console Listener Stopped ---", "SYSTEM");
            }
        }

        private void OnVbanReply(string reply)
        {
            // Must use the dispatcher to access UI and local fields safely
            if (!_isConsoleSilent)
            { 
                Dispatcher.Invoke(() =>
            {
                // First, add the raw reply to the history log
                AddToHistory(reply, "REPLY");

                // --- NEW LOGIC ---
                if (_isWaitingForConfigFile && reply.StartsWith("Command.Load"))
                {
                    _isWaitingForConfigFile = false; // Unset the flag

                    var match = Regex.Match(reply, "\"([^\"]*)\"");
                    if (match.Success)
                    {
                        string filePath = match.Groups[1].Value;
                        AddToHistory($"--- Config file found at: {filePath} ---", "SYSTEM");
                        try
                        {
                            var newPreset = ParseLiveStateFromXml(filePath);
                            _presets.Add(newPreset);
                            UpdatePresetManagerUI();
                            AddToHistory($"--- Successfully imported '{newPreset.Name}' from live state. ---", "SYSTEM");
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Failed to parse live state file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            });
        }
        }

        private Preset ParseLiveStateFromXml(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            if (doc.Root == null) throw new InvalidDataException("Config file is empty or invalid.");

            // Create a default name for the imported preset
            var preset = new Preset { Name = $"Live Import {DateTime.Now:yyyy-MM-dd HH:mm:ss}" };
            preset.VbanIndex = _presets.Count + 1; // Assign next available index

            var inputMap = new Dictionary<string, string> { { "1", "FL" }, { "2", "FR" }, { "3", "C" }, { "4", "S" }, { "5", "RL" }, { "6", "RR" } };

            // Target the correct section for live state
            var gridConfig = doc.Descendants("VBAudioMatrixGridConfiguration").FirstOrDefault();
            if (gridConfig == null) throw new InvalidDataException("'<VBAudioMatrixGridConfiguration>' section not found in config file.");

            // Group all <Point> elements (not <PresetPoint>) by their 'in' attribute
            var groupedPoints = gridConfig.Descendants("Point")
                .Where(p => p.Attribute("in") != null && inputMap.ContainsKey(p.Attribute("in").Value))
                .GroupBy(p => p.Attribute("in").Value);

            foreach (var group in groupedPoints.OrderBy(g => g.Key))
            {
                string label = inputMap[group.Key];
                var commandBases = group.Select(p => $"Point({p.Attribute("slotin")?.Value}.IN[{p.Attribute("in")?.Value}],{p.Attribute("slotout")?.Value}[{p.Attribute("out")?.Value}])").ToList();
                var gains = group.Select(p => {
                    double.TryParse(p.Attribute("dBGain")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double gainVal);
                    return gainVal;
                }).ToList();

                preset.Controls.Add(new MatrixControl { Label = label, CommandBases = commandBases, InitialGains = gains });
            }
            return preset;
        }

        private async void ScanMatrixGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_consoleVbanClient == null)
            {
                System.Windows.MessageBox.Show("VBAN is not active.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AddToHistory("--- Starting Full Matrix Grid Scan... ---", "SYSTEM");

            var replies = new List<string>();
            string filePath = string.Empty;

            // Use a TaskCompletionSource for robust async waiting
            var tcs = new TaskCompletionSource<string>();

            Action<string> replyHandler = (reply) => {
                if (reply.StartsWith("Command.Load"))
                {
                    var match = Regex.Match(reply, "\"([^\"]*)\"");
                    if (match.Success)
                    {
                        tcs.TrySetResult(match.Groups[1].Value);
                    }
                }
                // Also collect all other replies that might come in during the scan
                lock (replies) { replies.Add(reply); }
            };
            _consoleVbanClient.OnTextReplyReceived += replyHandler;

            // --- STAGE 1: Get Config File Path ---
            _consoleVbanClient.SendCommand("Command.Load = ?");

            // Asynchronously wait for our specific reply, with a timeout
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            if (completedTask == tcs.Task)
            {
                filePath = await tcs.Task; // Get the result
            }
            else
            {
                AddToHistory("Could not determine config file path (timeout). Aborting scan.", "SYSTEM");
                _consoleVbanClient.OnTextReplyReceived -= replyHandler;
                return;
            }

            // --- STAGE 2: Parse XML to find Active Slots ---
            var activeInputs = new List<string>();
            var activeOutputs = new List<string>();
            try
            {
                XDocument doc = XDocument.Load(filePath);
                if (doc.Root == null) throw new InvalidDataException("Config XML is invalid.");

                activeOutputs.AddRange(doc.Descendants("AMDevice").Select(d => d.Attribute("uniq")?.Value).Where(v => v != null)!);
                activeInputs.AddRange(doc.Descendants("VAIOSlot").Where(s => s.Attribute("online")?.Value == "1").Select(s => s.Attribute("uniq")?.Value).Where(v => v != null)!);

                AddToHistory($"Found {activeInputs.Count} active inputs and {activeOutputs.Count} active outputs.", "SYSTEM");
            }
            catch (Exception ex)
            {
                AddToHistory($"Error parsing config file: {ex.Message}", "SYSTEM");
                _consoleVbanClient.OnTextReplyReceived -= replyHandler;
                return;
            }

            // Clear old replies before we start the main scan
            lock (replies) { replies.Clear(); }

            // --- STAGE 3: Query Every Crosspoint ---
            AddToHistory($"Querying all crosspoints ({activeInputs.Count * 8 * activeOutputs.Count * 2} total)... (this may take a moment)", "SYSTEM");
            foreach (var input in activeInputs)
            {
                for (int i = 1; i <= 8; i++) // Assuming 8 channels per input
                {
                    foreach (var output in activeOutputs)
                    {
                        for (int j = 1; j <= 2; j++) // Assuming 2 channels per output
                        {
                            _consoleVbanClient.SendCommand($"Point({input}.IN[{i}],{output}[{j}]).dBGain = ?");
                            await Task.Delay(5); // Smallest possible delay
                        }
                    }
                }
            }

            // Wait for all replies to come in
            await Task.Delay(4000); // Increased wait time for the large number of packets
            _consoleVbanClient.OnTextReplyReceived -= replyHandler;

            // --- STAGE 4: Render the Output Nicely ---
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("--- MATRIX GRID SCAN RESULTS ---");
            bool foundAny = false;
            lock (replies)
            {
                // Order the results for readability
                foreach (var reply in replies.Where(r => r.StartsWith("Point")).OrderBy(r => r))
                {
                    if (!reply.Contains("-inf"))
                    {
                        summary.AppendLine(reply.Trim(';'));
                        foundAny = true;
                    }
                }
            }
            if (!foundAny) summary.AppendLine("No active crosspoints found.");
            summary.AppendLine("-----------------------------");
            AddToHistory(summary.ToString(), "SYSTEM");
        }
        private void AddToHistory(string text, string type)
        {
            var item = new ListBoxItem { DataContext = text };
            string prefix = "";
            System.Windows.Media.Brush foreground = System.Windows.Media.Brushes.WhiteSmoke;

            switch (type)
            {
                case "SENT":
                    prefix = ">";
                    foreground = System.Windows.Media.Brushes.Gray;
                    item.MouseDoubleClick += HistoryItem_MouseDoubleClick;
                    break;
                case "REPLY":
                    prefix = "";
                    foreground = System.Windows.Media.Brushes.LightGreen;
                    item.MouseDoubleClick += ReplyItem_MouseDoubleClick;
                    // --- THE FIX ---
                    // 1. Get the ContextMenu resource
                    var contextMenu = (ContextMenu)FindResource("LogItemContextMenu");
                    // 2. Find the MenuItem inside it
                    if (contextMenu.Items.Count > 0 && contextMenu.Items[0] is MenuItem saveMenuItem)
                    {
                        // 3. Attach our local event handler
                        saveMenuItem.Click += SaveLogItem_Click;
                        // 4. Assign the menu to our item
                        item.ContextMenu = contextMenu;
                    }
                    break;
                case "SYSTEM":
                    prefix = "SYSTEM:";
                    foreground = System.Windows.Media.Brushes.Gray;
                    item.MouseDoubleClick += ReplyItem_MouseDoubleClick;
                    // Do the same for SYSTEM messages
                    var sysContextMenu = (ContextMenu)FindResource("LogItemContextMenu");
                    if (sysContextMenu.Items.Count > 0 && sysContextMenu.Items[0] is MenuItem sysSaveMenuItem)
                    {
                        sysSaveMenuItem.Click += SaveLogItem_Click;
                        item.ContextMenu = sysContextMenu;
                    }
                    break;
            }
            item.Content = $"{prefix} {text}";
            item.Foreground = foreground;

            HistoryListBox.Items.Add(item);
            HistoryListBox.ScrollIntoView(item);
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryListBox.Items.Clear();
            _sentHistory.Clear();
            _historyIndex = -1;
        }

        private void HistoryItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is string command) {
                CustomCommandTextBox.Text = command;
                CustomCommandTextBox.Focus();
                CustomCommandTextBox.CaretIndex = command.Length;
            }
        }

        private void ReplyItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is string replyText)
            {
                try
                {
                    System.Windows.Clipboard.SetText(replyText);
                    // Optional: Give the user some feedback
                    AddToHistory($"Copied to clipboard: '{replyText}'", "SYSTEM");
                }
                catch (Exception ex)
                {
                    AddToHistory($"Failed to copy to clipboard: {ex.Message}", "SYSTEM");
                }
            }
        }

        private void CustomCommandTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { SendCustomCommand_Click(sender, e); e.Handled = true; return; }
            if (_sentHistory.Count == 0) return;
            if (e.Key == Key.Up) {
                _historyIndex = Math.Min(_sentHistory.Count - 1, _historyIndex + 1);
                CustomCommandTextBox.Text = _sentHistory[_sentHistory.Count - 1 - _historyIndex];
            } else if (e.Key == Key.Down) {
                _historyIndex = Math.Max(-1, _historyIndex - 1);
                CustomCommandTextBox.Text = _historyIndex == -1 ? "" : _sentHistory[_sentHistory.Count - 1 - _historyIndex];
            }
            CustomCommandTextBox.CaretIndex = CustomCommandTextBox.Text.Length;
            e.Handled = true;
        }

        private void PaletteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string command) {
                CustomCommandTextBox.Text = command;
                CustomCommandTextBox.Focus();
                CustomCommandTextBox.CaretIndex = command.Length;
            }
        }


        private async void QueryAll_Click(object sender, RoutedEventArgs e)
        {
            if (_consoleVbanClient == null)
            {
                System.Windows.MessageBox.Show("VBAN is not active.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



            SyncButton.IsEnabled = false; // Disable the button
            SyncButtonText.Visibility = Visibility.Collapsed;
            SyncButtonSpinner.Visibility = Visibility.Visible;
            _isConsoleSilent = true;


            try
            {
                AddToHistory("--- Starting Master System Diagnostics... ---", "SYSTEM");

                // --- STAGE 1: System Info and Preset Discovery ---
                var systemReplies = new List<string>();
                var receivedEvent = new System.Threading.AutoResetEvent(false);
                Action<string> replyHandler = (reply) => { lock (systemReplies) { systemReplies.Add(reply); } receivedEvent.Set(); };
                _consoleVbanClient.OnTextReplyReceived += replyHandler;

                _consoleVbanClient.SendCommand("Command.Version = ?");
                _consoleVbanClient.SendCommand("Command.Load = ?");
                _consoleVbanClient.SendCommand("Command.Engine = ?");
                _consoleVbanClient.SendCommand("Command.Master = ?");
                for (int i = 1; i <= 16; i++)
                {
                    _consoleVbanClient.SendCommand($"PresetPatch[{i}].Name = ?");
                }
                await Task.Delay(1500);
                _consoleVbanClient.OnTextReplyReceived -= replyHandler; // We are done with this handler

                var summary = new System.Text.StringBuilder();
                summary.AppendLine("--- MASTER DIAGNOSTIC REPORT ---");
                Func<string, string, string> GetReplyValue = (prefix, defaultValue) =>
                {
                    var reply = systemReplies.FirstOrDefault(r => r.StartsWith(prefix));
                    if (reply != null) { var parts = reply.Split(new[] { '=' }, 2); return parts.Length > 1 ? parts[1].Trim(' ', ';', '"') : defaultValue; }
                    return defaultValue;
                };
                summary.AppendLine($"Version: {GetReplyValue("Command.Version", "N/A")}");
                summary.AppendLine($"Config File: {GetReplyValue("Command.Load", "N/A")}");
                summary.AppendLine($"Engine: {GetReplyValue("Command.Engine", "N/A")}");
                summary.AppendLine($"Master: {GetReplyValue("Command.Master", "N/A")}");
                summary.AppendLine();

                var discoveredPresets = new Dictionary<int, string>();
                lock (systemReplies)
                {
                    foreach (var reply in systemReplies.Where(r => r.StartsWith("PresetPatch")))
                    {
                        var nameMatch = Regex.Match(reply, "\"([^\"]*)\"");
                        var indexMatch = Regex.Match(reply, @"PresetPatch\[(\d+)\]");
                        if (nameMatch.Success && !string.IsNullOrEmpty(nameMatch.Groups[1].Value) && indexMatch.Success && int.TryParse(indexMatch.Groups[1].Value, out int index))
                        {
                            discoveredPresets[index] = nameMatch.Groups[1].Value;
                        }
                    }
                }

                // --- STAGE 2: Deep Scan Each Discovered Preset ---
                foreach (var presetPair in discoveredPresets.OrderBy(p => p.Key))
                {
                    int presetIndex = presetPair.Key;
                    string presetName = presetPair.Value;
                    summary.AppendLine($"-- Preset {presetIndex} --");
                    AddToHistory($"Applying and scanning Preset {presetIndex}...", "SYSTEM");

                    var presetReplies = new List<string>();
                    Action<string> presetReplyHandler = (reply) => { lock (presetReplies) { presetReplies.Add(reply); } };
                    _consoleVbanClient.OnTextReplyReceived += presetReplyHandler;

                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Recall;PresetPatch[{presetIndex}].Select");

                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Name = ?");
                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Comment = ?");
                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Gain = ?");
                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Point = ?");
                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Zone = ?");
                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Mute = ?");

                    string filePath = GetReplyValue("Command.Load", "");
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        XDocument doc = XDocument.Load(filePath);
                        var activeOutputs = doc.Descendants("AMDevice").Select(d => d.Attribute("uniq")?.Value).Where(v => v != null).ToList();
                        var activeInputs = doc.Descendants("VAIOSlot").Where(s => s.Attribute("online")?.Value == "1").Select(s => s.Attribute("uniq")?.Value).Where(v => v != null).ToList();
                        foreach (var input in activeInputs)
                        {
                            for (int i = 1; i <= 8; i++)
                            {
                                foreach (var output in activeOutputs)
                                {
                                    for (int j = 1; j <= 8; j++)
                                    {
                                        _consoleVbanClient.SendCommand($"Point({input}.IN[{i}],{output}[{j}]).dBGain = ?");
                                    }
                                }
                            }
                        }
                    }

                    await Task.Delay(3000);
                    _consoleVbanClient.OnTextReplyReceived -= presetReplyHandler;

                    Func<string, string, string> GetPresetReplyValue = (prefix, defaultValue) =>
                    {
                        var reply = presetReplies.FirstOrDefault(r => r.StartsWith(prefix));
                        if (reply != null) { var parts = reply.Split(new[] { '=' }, 2); return parts.Length > 1 ? parts[1].Trim(' ', ';', '"') : defaultValue; }
                        return defaultValue;
                    };

                    summary.AppendLine($"  Name: {GetPresetReplyValue($"PresetPatch[{presetIndex}].Name", "N/A")}");
                    summary.AppendLine($"  Comment: {GetPresetReplyValue($"PresetPatch[{presetIndex}].Comment", "N/A")}");
                    summary.AppendLine($"  Gain: {GetPresetReplyValue($"PresetPatch[{presetIndex}].Gain", "N/A")}");
                    summary.AppendLine($"  Points: {GetPresetReplyValue($"PresetPatch[{presetIndex}].Point", "N/A")}");
                    summary.AppendLine($"  Zones: {GetPresetReplyValue($"PresetPatch[{presetIndex}].Zone", "N/A")}");
                    summary.AppendLine($"  Muted/Total: {GetPresetReplyValue($"PresetPatch[{presetIndex}].Mute", "N/A")}");

                    summary.AppendLine();
                    summary.AppendLine("--- MATRIX GRID SCAN RESULTS ---");
                    var activePoints = new List<string>();
                    lock (presetReplies)
                    {
                        foreach (var reply in presetReplies.Where(r => r.StartsWith("Point")))
                        {
                            // Split the reply at the '=' sign
                            var parts = reply.Split(new[] { '=' }, 2);
                            if (parts.Length > 1)
                            {
                                // Get the value part and trim it
                                string value = parts[1].Trim(' ', ';');

                                // Only add the point if the value is NOT empty and NOT "-inf"
                                if (!string.IsNullOrEmpty(value) && value != "-inf")
                                {
                                    activePoints.Add(reply.Trim(';'));
                                }
                            }
                        }
                    }

                    foreach (var point in activePoints.OrderBy(p => p))
                    {
                        summary.AppendLine(point);
                    }

                    string reportedPoints = GetPresetReplyValue($"PresetPatch[{presetIndex}].Point", "0");
                    summary.AppendLine("-----------------------------");
                    summary.AppendLine($"Points: {activePoints.Count}/{reportedPoints}");
                    summary.AppendLine("-----------------------------");
                    summary.AppendLine();
                }

                summary.AppendLine("--- END OF REPORT ---");
                AddToHistory(summary.ToString(), "SYSTEM");
            }
            finally
            {
                _isConsoleSilent = false;
                SyncButtonSpinner.Visibility = Visibility.Collapsed;
                SyncButtonText.Visibility = Visibility.Visible;
                SyncButton.IsEnabled = true;
            }
            
        }

        private async void SyncPresets_Click(object sender, RoutedEventArgs e)
        {
            if (_consoleVbanClient == null)
            {
                System.Windows.MessageBox.Show("VBAN is not active.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SyncButton.IsEnabled = false;
            SyncButtonText.Visibility = Visibility.Collapsed;
            SyncButtonSpinner.Visibility = Visibility.Visible;
            _isConsoleSilent = true;
            try
            {
                AddToHistory("--- Starting Preset Sync... ---", "SYSTEM");
                _presets.Clear();
                UpdatePresetManagerUI();
                AddToHistory("Cleared local presets. Discovering new presets from Matrix...", "SYSTEM");

                var allReplies = new List<string>();
                Action<string> replyHandler = (reply) => { lock (allReplies) { allReplies.Add(reply); } };
                _consoleVbanClient.OnTextReplyReceived += replyHandler;

                _consoleVbanClient.SendCommand("Command.Load = ?");
                await Task.Delay(500);

                string filePath;
                var discoveredPresets = new Dictionary<int, string>();
                lock (allReplies)
                {
                    var pathReply = allReplies.FirstOrDefault(r => r.StartsWith("Command.Load"));
                    var match = pathReply != null ? Regex.Match(pathReply, "\"([^\"]*)\"") : null;
                    if (!match?.Success ?? true) { AddToHistory("Could not determine config file path. Aborting.", "SYSTEM"); _consoleVbanClient.OnTextReplyReceived -= replyHandler; return; }
                    filePath = match.Groups[1].Value;
                }
                allReplies.Clear();

                for (int i = 1; i <= 16; i++)
                {
                    _consoleVbanClient.SendCommand($"PresetPatch[{i}].Name = ?");
                }

                List<string> activeInputs;
                try
                {
                    XDocument doc = XDocument.Load(filePath);
                    if (doc.Root == null) throw new InvalidDataException("Config XML is invalid.");
                    activeInputs = doc.Descendants("VAIOSlot").Where(s => s.Attribute("online")?.Value == "1").Select(s => s.Attribute("uniq")?.Value).Where(v => v != null).ToList();
                }
                catch (Exception ex) { AddToHistory($"Error parsing config file: {ex.Message}", "SYSTEM"); _consoleVbanClient.OnTextReplyReceived -= replyHandler; return; }

                foreach (var inputSlot in activeInputs)
                {
                    for (int i = 1; i <= 8; i++) { _consoleVbanClient.SendCommand($"Input({inputSlot}.IN[{i}]).Name = ?"); }
                }
                await Task.Delay(250);
                _consoleVbanClient.OnTextReplyReceived -= replyHandler;

                var dynamicInputMap = new Dictionary<string, string>();
                lock (allReplies)
                {
                    foreach (var reply in allReplies.Where(r => r.StartsWith("Input")))
                    {
                        var nameMatch = Regex.Match(reply, "\"([^\"]*)\"");
                        var indexMatch = Regex.Match(reply, @"\.IN\[(\d+)\]");
                        if (nameMatch.Success && indexMatch.Success && !string.IsNullOrEmpty(nameMatch.Groups[1].Value))
                        {
                            dynamicInputMap[indexMatch.Groups[1].Value] = nameMatch.Groups[1].Value;
                        }
                    }
                    foreach (var reply in allReplies.Where(r => r.StartsWith("PresetPatch")))
                    {
                        var nameMatch = Regex.Match(reply, "\"([^\"]*)\"");
                        var indexMatch = Regex.Match(reply, @"PresetPatch\[(\d+)\]");
                        if (nameMatch.Success && !string.IsNullOrEmpty(nameMatch.Groups[1].Value) && indexMatch.Success && int.TryParse(indexMatch.Groups[1].Value, out int index))
                        {
                            discoveredPresets[index] = nameMatch.Groups[1].Value;
                        }
                    }
                }

                var hardcodedInputMap = new Dictionary<string, string> { { "1", "FL" }, { "2", "FR" }, { "3", "C" }, { "4", "S" }, { "5", "RL" }, { "6", "RR" } };

                var namesOfActiveChannels = new List<string>();
                foreach (var key in dynamicInputMap.Keys)
                {
                    if (hardcodedInputMap.ContainsKey(key))
                    {
                        namesOfActiveChannels.Add(dynamicInputMap[key]);
                    }
                }
                bool useDynamicNames = namesOfActiveChannels.Count > 0 && namesOfActiveChannels.Count == namesOfActiveChannels.Distinct().Count();
                var finalInputMap = useDynamicNames ? dynamicInputMap : hardcodedInputMap;
                AddToHistory(useDynamicNames ? "Using dynamic names from Matrix." : "Duplicate or empty names found. Using fallback.", "SYSTEM");

                var newPresets = new List<Preset>();
                List<string> activeWinOutputs;
                try
                {
                    XDocument doc = XDocument.Load(filePath);
                    if (doc.Root == null) throw new InvalidDataException("Config XML is invalid.");
                    activeWinOutputs = doc.Descendants("AMDevice").Where(d => d.Attribute("uniq")?.Value?.StartsWith("WIN") ?? false).Select(d => d.Attribute("uniq")?.Value).Where(v => v != null).ToList();
                }
                catch (Exception ex) { AddToHistory($"Error parsing config file: {ex.Message}", "SYSTEM"); return; }

                foreach (var presetPair in discoveredPresets.OrderBy(p => p.Key))
                {
                    int presetIndex = presetPair.Key;
                    string presetName = presetPair.Value;
                    var presetReplies = new List<string>();
                    Action<string> presetReplyHandler = (reply) => { lock (presetReplies) { presetReplies.Add(reply); } };
                    _consoleVbanClient.OnTextReplyReceived += presetReplyHandler;

                    _consoleVbanClient.SendCommand($"PresetPatch[{presetIndex}].Recall;PresetPatch[{presetIndex}].Select");
                    await Task.Delay(50);

                    foreach (var inputSlot in activeInputs)
                    {
                        foreach (var inputNum in finalInputMap.Keys)
                        {
                            foreach (var outputSlot in activeWinOutputs)
                            {
                                for (int j = 1; j <= 8; j++) { _consoleVbanClient.SendCommand($"Point({inputSlot}.IN[{inputNum}],{outputSlot}[{j}]).dBGain = ?"); }
                            }
                        }
                    }

                    await Task.Delay(750);
                    _consoleVbanClient.OnTextReplyReceived -= presetReplyHandler;

                    var currentPreset = new Preset { Name = presetName, VbanIndex = presetIndex };

                    var activePoints = presetReplies.Where(r => r.StartsWith("Point") && !r.Contains("-inf") && !r.EndsWith("=;")).ToList();

                    var controlsToBuild = new Dictionary<string, MatrixControl>();

                    foreach (var pointReply in activePoints)
                    {
                        var inMatch = Regex.Match(pointReply, @"\.IN\[(\d+)\]");
                        if (!inMatch.Success) continue;

                        string inputNumber = inMatch.Groups[1].Value;
                        if (!finalInputMap.TryGetValue(inputNumber, out string label)) continue;

                        if (!controlsToBuild.ContainsKey(label))
                        {
                            controlsToBuild[label] = new MatrixControl { Label = label };
                        }

                        var pointMatch = Regex.Match(pointReply, @"(Point\(.*\))\.dBGain\s*=\s*([-\d\.]+)");
                        if (pointMatch.Success)
                        {
                            controlsToBuild[label].CommandBases.Add(pointMatch.Groups[1].Value);
                            double.TryParse(pointMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double gain);
                            controlsToBuild[label].InitialGains.Add(gain);
                        }
                    }
                    currentPreset.Controls.AddRange(controlsToBuild.Values.OrderBy(c => c.Label));
                    newPresets.Add(currentPreset);
                }

                _presets = newPresets;
                Properties.Settings.Default.PresetsJson = JsonConvert.SerializeObject(_presets);
                Properties.Settings.Default.Save();
                UpdatePresetManagerUI();
                AddToHistory($"SUCCESS: Synced and saved {_presets.Count} presets from the live Matrix state.", "SYSTEM");
            }
            finally
            {
                _isConsoleSilent = true;
                SyncButtonSpinner.Visibility = Visibility.Collapsed;
                SyncButtonText.Visibility = Visibility.Visible;
                SyncButton.IsEnabled = true;
            }
        }
    
private void SaveLogItem_Click(object sender, RoutedEventArgs e)
        {
            // The sender is the MenuItem. Its DataContext is the ListBoxItem it was opened from.
            if (sender is MenuItem menuItem && menuItem.DataContext is ListBoxItem listBoxItem)
            {
                if (listBoxItem.DataContext is string contentToSave)
                {
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = "log_export.txt",
                        Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllText(saveFileDialog.FileName, contentToSave);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void SendCustomCommand_Click(object sender, RoutedEventArgs e)
        {
            if (_consoleVbanClient == null) return;
            if (!string.IsNullOrWhiteSpace(CustomCommandTextBox.Text)) {
                string command = CustomCommandTextBox.Text;
                _consoleVbanClient.SendCommand(command);
                AddToHistory(command, "SENT");
                if (!_sentHistory.Contains(command)) _sentHistory.Add(command);
                _historyIndex = -1;
                CustomCommandTextBox.Clear();
            }
        }
        
        private void PopulatePlaybackDevices()
        {
            DefaultDeviceComboBox.Items.Clear();
            var controller = new CoreAudioController();
            var playbackDevices = controller.GetPlaybackDevices(AudioSwitcher.AudioApi.DeviceState.Active);
            foreach (var device in playbackDevices) {
                var item = new ComboBoxItem { Content = device.FullName, Tag = device.Id.ToString() };
                DefaultDeviceComboBox.Items.Add(item);
            }
        }

        private void AutoSelectDeviceCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateDeviceComboBoxVisibility();
        }

        private void UpdateDeviceComboBoxVisibility()
        {
            DefaultDeviceComboBox.Visibility = AutoSelectDeviceCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}