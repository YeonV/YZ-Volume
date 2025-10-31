using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// Aliases
using Grid = System.Windows.Controls.Grid;
using Slider = System.Windows.Controls.Slider;
using TextBlock = System.Windows.Controls.TextBlock;
using MouseButtons = System.Windows.Forms.MouseButtons;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace YZ_Volume
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private MatrixUdpClient? _matrixClient;
        private List<Preset> _presets = new();

        public MainWindow()
        {
            InitializeComponent();
            DwmApi.UseImmersiveDarkMode(this, true);
            Deactivated += OnDeactivated;
            Loaded += OnLoaded;
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "YZ-Volume";
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico"))?.Stream;
            if (iconStream != null) _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            _notifyIcon.Visible = true;
            var contextMenu = new ContextMenu();
            var settingsItem = new MenuItem { Header = "Settings..." };
            settingsItem.Click += (s, e) => OpenSettingsWindow();
            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(exitItem);
            _notifyIcon.MouseClick += (sender, args) => {
                if (args.Button == MouseButtons.Left)
                {
                    if (IsVisible) Hide();
                    else
                    {
                        Show();
                        var desktopWorkingArea = SystemParameters.WorkArea;
                        Left = desktopWorkingArea.Right - ActualWidth;
                        Top = desktopWorkingArea.Bottom - ActualHeight;
                        Activate();
                    }
                }
                else if (args.Button == MouseButtons.Right)
                {
                    contextMenu.IsOpen = true;
                    Activate();
                }
            };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeVbanClient();

            if (Properties.Settings.Default.VbanEnabled)
            {
                string lastName = Properties.Settings.Default.LastSelectedPresetName;
                int lastIndex = _presets.FindIndex(p => p.Name == lastName);
                if (lastIndex == -1) lastIndex = 0;

                if (lastIndex >= 0 && lastIndex < PresetComboBox.Items.Count)
                {
                    PresetComboBox.SelectionChanged -= PresetComboBox_SelectionChanged;
                    PresetComboBox.SelectedIndex = lastIndex;
                    PresetComboBox.SelectionChanged += PresetComboBox_SelectionChanged;

                    ApplyPreset(lastIndex, false);
                }
            }
            else
            {
                RefreshAllControls();
            }
        }

        private void InitializeVbanClient()
        {
            if (Properties.Settings.Default.VbanEnabled)
            {
                _presets = GetPresetsFromSettings();

                PresetComboBox.Items.Clear();
                foreach (var preset in _presets)
                {
                    PresetComboBox.Items.Add(preset.Name);
                }
                PresetComboBox.Visibility = Visibility.Visible;

                if (_matrixClient == null)
                {
                    var settings = Properties.Settings.Default;
                    _matrixClient = new MatrixUdpClient(settings.VbanIpAddress, settings.VbanPort, settings.VbanStreamName);
                    _matrixClient.StartListener();
                }
            }
            else
            {
                PresetComboBox.Visibility = Visibility.Collapsed;
                if (_matrixClient != null)
                {
                    _matrixClient.StopListener();
                    _matrixClient = null;
                }
            }
        }

        private List<Preset> GetPresetsFromSettings()
        {
            string json = Properties.Settings.Default.PresetsJson;
            if (string.IsNullOrEmpty(json))
            {
                var settingsWindow = new SettingsWindow();
                var defaultPresets = settingsWindow.GetDefaultPresets();
                Properties.Settings.Default.PresetsJson = JsonConvert.SerializeObject(defaultPresets);
                Properties.Settings.Default.Save();
                return defaultPresets;
            }
            return JsonConvert.DeserializeObject<List<Preset>>(json) ?? new List<Preset>();
        }

        private void RefreshAllControls()
        {
            LoadAudioDevices();
            LoadMatrixControls();
        }

        private void LoadAudioDevices()
        {
            DeviceListPanel.Children.Clear();
            var enumerator = new MMDeviceEnumerator();
            var savedDeviceIDs = Properties.Settings.Default.VisibleDeviceIDs;
            if (savedDeviceIDs == null) return;
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).Union(enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active));
            foreach (var device in devices)
            {
                if (savedDeviceIDs.Contains(device.ID)) AddDeviceToUI(device);
            }
        }

        private void AddDeviceToUI(MMDevice device)
        {
            var deviceGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var muteButton = new System.Windows.Controls.Primitives.ToggleButton { Style = (Style)FindResource("MuteToggleButtonStyle"), IsChecked = device.AudioEndpointVolume.Mute, VerticalAlignment = VerticalAlignment.Center };

            string displayName = device.FriendlyName;
            string customNamesJson = Properties.Settings.Default.CustomDeviceNames;
            if (!string.IsNullOrEmpty(customNamesJson))
            {
                var customNamesDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(customNamesJson) ?? new Dictionary<string, string>();
                if (customNamesDict.ContainsKey(device.ID) && !string.IsNullOrWhiteSpace(customNamesDict[device.ID]))
                {
                    displayName = customNamesDict[device.ID];
                }
            }
            var nameLabel = new TextBlock { Text = displayName, Foreground = System.Windows.Media.Brushes.WhiteSmoke, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0), TextTrimming = TextTrimming.CharacterEllipsis };

            var volumeSlider = new Slider { Minimum = 0, Maximum = 100, Value = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100), Style = (Style)FindResource("UltimateSliderStyle") };

            volumeSlider.ValueChanged += (sender, args) => { device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)(args.NewValue / 100.0); };
            muteButton.Click += (sender, args) => { device.AudioEndpointVolume.Mute = muteButton.IsChecked ?? false; };

            Grid.SetColumn(muteButton, 0);
            Grid.SetColumn(nameLabel, 1);
            Grid.SetColumn(volumeSlider, 2);
            deviceGrid.Children.Add(muteButton);
            deviceGrid.Children.Add(nameLabel);
            deviceGrid.Children.Add(volumeSlider);
            DeviceListPanel.Children.Add(deviceGrid);
        }

        private void LoadMatrixControls()
        {
            MatrixControlsPanel.Children.Clear();
            int presetIndex = PresetComboBox.SelectedIndex;
            if (_matrixClient == null || presetIndex < 0 || presetIndex >= _presets.Count)
            {
                MatrixSeparator.Visibility = Visibility.Collapsed;
                return;
            }
            MatrixSeparator.Visibility = Visibility.Visible;
            var preset = _presets[presetIndex];
            foreach (var control in preset.Controls)
            {
                AddMatrixControlToUI(control);
            }
        }

        private void AddMatrixControlToUI(MatrixControl control)
        {
            // --- UI LAYOUT: 5 Columns for [Mute] [Label] [Slider] [-] [+] ---
            var deviceGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 15) };
            deviceGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto }); // Mute
            deviceGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(85) });   // Label
            deviceGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Slider
            deviceGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto }); // Nudge Down
            deviceGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto }); // Nudge Up

            // --- CREATE CONTROLS (FULLY QUALIFIED) ---
            var muteButton = new System.Windows.Controls.Primitives.ToggleButton
            {
                Style = (Style)FindResource("MuteToggleButtonStyle"),
                VerticalAlignment = VerticalAlignment.Center
            };

            var nameLabel = new System.Windows.Controls.TextBlock
            {
                Text = control.Label,
                Foreground = System.Windows.Media.Brushes.WhiteSmoke,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var volumeSlider = new System.Windows.Controls.Slider
            {
                Minimum = -100,
                Maximum = 0,
                Value = control.InitialGains.FirstOrDefault(),
                Style = (Style)FindResource("UltimateSliderStyle"),
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                VerticalAlignment = VerticalAlignment.Center
            };

            var nudgeDownButton = new System.Windows.Controls.Button
            {
                Content = "-",
                Style = (Style)FindResource("NudgeButtonStyle"),
                Margin = new Thickness(5, 0, 2, 0),
                ToolTip = "Nudge Gain -1 dB"
            };

            var nudgeUpButton = new System.Windows.Controls.Button
            {
                Content = "+",
                Style = (Style)FindResource("NudgeButtonStyle"),
                Margin = new Thickness(2, 0, 0, 0),
                ToolTip = "Nudge Gain +1 dB"
            };

            // --- WIRE UP EVENT HANDLERS ---
            volumeSlider.ValueChanged += (sender, args) => {
                var commands = new List<string>();
                foreach (var commandBase in control.CommandBases)
                {
                    commands.Add($"{commandBase}.dBGain = {((int)args.NewValue)}");
                }
                SendVbanCommand(string.Join(";", commands));
            };

            muteButton.Click += (sender, args) => {
                string muteValue = muteButton.IsChecked == true ? "1" : "0";
                var commands = new List<string>();
                foreach (var commandBase in control.CommandBases)
                {
                    commands.Add($"{commandBase}.Mute = {muteValue}");
                }
                SendVbanCommand(string.Join(";", commands));
            };

            nudgeDownButton.Click += (sender, args) => {
                var commands = new List<string>();
                foreach (var commandBase in control.CommandBases)
                {
                    commands.Add($"{commandBase}.dBGain += -1.0");
                }
                SendVbanCommand(string.Join(";", commands));
            };

            nudgeUpButton.Click += (sender, args) => {
                var commands = new List<string>();
                foreach (var commandBase in control.CommandBases)
                {
                    commands.Add($"{commandBase}.dBGain += 1.0");
                }
                SendVbanCommand(string.Join(";", commands));
            };

            // --- PLACE CONTROLS IN GRID (FULLY QUALIFIED) ---
            System.Windows.Controls.Grid.SetColumn(muteButton, 0);
            System.Windows.Controls.Grid.SetColumn(nameLabel, 1);
            System.Windows.Controls.Grid.SetColumn(volumeSlider, 2);
            System.Windows.Controls.Grid.SetColumn(nudgeDownButton, 3);
            System.Windows.Controls.Grid.SetColumn(nudgeUpButton, 4);

            deviceGrid.Children.Add(muteButton);
            deviceGrid.Children.Add(nameLabel);
            deviceGrid.Children.Add(volumeSlider);
            deviceGrid.Children.Add(nudgeDownButton);
            deviceGrid.Children.Add(nudgeUpButton);

            MatrixControlsPanel.Children.Add(deviceGrid);
        }
        private void SendVbanCommand(string command)
        {
            if (_matrixClient != null)
            {
                // Send the entire command string (semicolons and all) in one single packet.
                _matrixClient.SendCommand(command);
                System.Diagnostics.Debug.WriteLine($"Sent VBAN Command: {command}");
            }
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null || _matrixClient == null) return;
            int presetIndex = PresetComboBox.SelectedIndex;
            if (presetIndex == -1) return;
            ApplyPreset(presetIndex, true);
        }

        private void ApplyPreset(int presetIndex, bool sendCommands)
        {
            if (presetIndex < 0 || presetIndex >= _presets.Count) return;
            if (sendCommands && _matrixClient != null)
            {
                int commandIndex = presetIndex + 1;
                SendVbanCommand("Command.ResetGrid");
                System.Threading.Thread.Sleep(50);
                SendVbanCommand($"PresetPatch[{commandIndex}].Apply");
                System.Threading.Thread.Sleep(50);
                SendVbanCommand($"PresetPatch[{commandIndex}].Select");
            }

            RefreshAllControls();

            Properties.Settings.Default.LastSelectedPresetName = _presets[presetIndex].Name;
            Properties.Settings.Default.Save();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

        private void OpenSettingsWindow()
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            if (settingsWindow.ShowDialog() == true)
            {
                InitializeVbanClient();
                RefreshAllControls();
            }
        }

        private void OnDeactivated(object? sender, EventArgs e) => Hide();
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }

        private void VbanClient_OnStateUpdated(VoicemeeterState newState) { /* Placeholder for future RT-Packet implementation */ }
    }
}