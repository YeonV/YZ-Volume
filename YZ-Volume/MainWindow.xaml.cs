using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
// Aliases
using Grid = System.Windows.Controls.Grid;
using MouseButtons = System.Windows.Forms.MouseButtons;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using Slider = System.Windows.Controls.Slider;
using TextBlock = System.Windows.Controls.TextBlock;

namespace YZ_Volume
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private MatrixUdpClient? _matrixClient;
        private List<Preset> _presets = new();
        private Dictionary<string, Slider> _matrixChannelSliders = new();
        private Dictionary<Slider, RoutedPropertyChangedEventHandler<double>> _sliderEventHandlers = new();

        public MainWindow()
        {
            InitializeComponent();
            DwmApi.UseImmersiveDarkMode(this, true);
            Deactivated += OnDeactivated;
            Loaded += OnLoaded;
            InitializeNotifyIcon();

            MasterVolumeSlider.ValueChanged += MasterVolumeSlider_ValueChanged;
            MasterMuteButton.Click += MasterMuteButton_Click;
            MasterNudgeDownButton.Click += MasterNudgeDownButton_Click;
            MasterNudgeUpButton.Click += MasterNudgeUpButton_Click;
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

            if (Properties.Settings.Default.VbanEnabled && _presets.Any())
            {
                string lastName = Properties.Settings.Default.LastSelectedPresetName;
                var lastPreset = _presets.FirstOrDefault(p => p.Name == lastName) ?? _presets.First();
                int displayIndex = _presets.IndexOf(lastPreset);
                if (displayIndex != -1)
                {
                    PresetComboBox.SelectionChanged -= PresetComboBox_SelectionChanged;
                    PresetComboBox.SelectedIndex = displayIndex;
                    PresetComboBox.SelectionChanged += PresetComboBox_SelectionChanged;
                    ApplyPreset(lastPreset, false);
                }
            }
            else { RefreshAllControls(); }
        }

        private void InitializeVbanClient()
        {
            if (Properties.Settings.Default.VbanEnabled)
            {
                _presets = GetPresetsFromSettings();
                PresetComboBox.Items.Clear();
                foreach (var preset in _presets) { PresetComboBox.Items.Add(preset.Name); }
                PresetComboBox.Visibility = Visibility.Visible;
                MasterSliderGrid.Visibility = Visibility.Visible;
                if (_matrixClient == null)
                {
                    var settings = Properties.Settings.Default;
                    _matrixClient = new MatrixUdpClient(settings.VbanIpAddress, settings.VbanPort, settings.VbanStreamName);
                    _matrixClient.StartListener();
                }
            }
            else
            {
                MasterSliderGrid.Visibility = Visibility.Collapsed;
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
                var settingsWindow = new SettingsWindow(null);
                var defaultPresets = settingsWindow.GetDefaultPresets();
                Properties.Settings.Default.PresetsJson = JsonConvert.SerializeObject(defaultPresets);
                Properties.Settings.Default.Save();
                return defaultPresets;
            }
            return JsonConvert.DeserializeObject<List<Preset>>(json) ?? new List<Preset>();
        }

        private void RefreshAllControls()
        {
            _matrixChannelSliders.Clear();
            _sliderEventHandlers.Clear();
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
                if (customNamesDict.ContainsKey(device.ID) && !string.IsNullOrWhiteSpace(customNamesDict[device.ID])) { displayName = customNamesDict[device.ID]; }
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
            var currentControl = control;
            var deviceGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var muteButton = new System.Windows.Controls.Primitives.ToggleButton { Style = (Style)FindResource("MuteToggleButtonStyle"), VerticalAlignment = VerticalAlignment.Center };
            var nameLabel = new TextBlock { Text = currentControl.Label, Foreground = System.Windows.Media.Brushes.WhiteSmoke, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0), TextTrimming = TextTrimming.CharacterEllipsis };

            double initialGain = currentControl.InitialGains.FirstOrDefault();
            var volumeSlider = new Slider
            {
                Minimum = -100,
                Maximum = 20,
                Value = initialGain,
                Style = (Style)FindResource("UltimateSliderStyle"),
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = initialGain
            };

            var nudgeDownButton = new System.Windows.Controls.Button { Content = "-", Style = (Style)FindResource("NudgeButtonStyle"), Margin = new Thickness(5, 0, 2, 0), ToolTip = "Nudge Gain -1 dB" };
            var nudgeUpButton = new System.Windows.Controls.Button { Content = "+", Style = (Style)FindResource("NudgeButtonStyle"), Margin = new Thickness(2, 0, 0, 0), ToolTip = "Nudge Gain +1 dB" };

            RoutedPropertyChangedEventHandler<double> valueChangedHandler = (sender, args) => {
                var commands = new List<string>();
                foreach (var commandBase in currentControl.CommandBases) { commands.Add($"{commandBase}.dBGain = {((int)args.NewValue)}"); }
                SendVbanCommand(string.Join(";", commands));
            };
            volumeSlider.ValueChanged += valueChangedHandler;
            _sliderEventHandlers[volumeSlider] = valueChangedHandler;

            muteButton.Click += (sender, args) => {
                string muteValue = muteButton.IsChecked == true ? "1" : "0";
                var commands = new List<string>();
                foreach (var commandBase in currentControl.CommandBases) { commands.Add($"{commandBase}.Mute = {muteValue}"); }
                SendVbanCommand(string.Join(";", commands));
            };
            nudgeDownButton.Click += (sender, args) => {
                var commands = new List<string>();
                foreach (var commandBase in currentControl.CommandBases) { commands.Add($"{commandBase}.dBGain += -1.0"); }
                SendVbanCommand(string.Join(";", commands));
            };
            nudgeUpButton.Click += (sender, args) => {
                var commands = new List<string>();
                foreach (var commandBase in currentControl.CommandBases) { commands.Add($"{commandBase}.dBGain += 1.0"); }
                SendVbanCommand(string.Join(";", commands));
            };

            if (currentControl.CommandBases.Any())
            {
                _matrixChannelSliders[currentControl.CommandBases.First()] = volumeSlider;
            }

            Grid.SetColumn(muteButton, 0);
            Grid.SetColumn(nameLabel, 1);
            Grid.SetColumn(volumeSlider, 2);
            Grid.SetColumn(nudgeDownButton, 3);
            Grid.SetColumn(nudgeUpButton, 4);
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
                var individualCommands = command.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var individualCommand in individualCommands)
                {
                    _matrixClient.SendCommand(individualCommand.Trim());
                    System.Diagnostics.Debug.WriteLine($"Sent VBAN Command: {individualCommand.Trim()}");
                }
            }
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null || _matrixClient == null) return;
            int presetIndex = PresetComboBox.SelectedIndex;
            if (presetIndex == -1) return;
            var selectedPreset = _presets[presetIndex];
            ApplyPreset(selectedPreset, true);
        }

        private void RefreshPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null) return;

            // Find the currently selected preset object
            var selectedPreset = _presets.FirstOrDefault(p => p.Name == PresetComboBox.SelectedItem.ToString());

            if (selectedPreset != null)
            {
                // Call ApplyPreset but tell it NOT to send VBAN commands,
                // only to refresh the UI sliders to their initial state.
                ApplyPreset(selectedPreset, false);
            }
        }

        private void ApplyPreset(Preset preset, bool sendCommands)
        {
            if (preset == null) return;
            if (sendCommands && _matrixClient != null)
            {
                int commandIndex = preset.VbanIndex;
                SendVbanCommand("Command.ResetGrid");
                System.Threading.Thread.Sleep(50);
                SendVbanCommand($"PresetPatch[{commandIndex}].Apply");
                System.Threading.Thread.Sleep(50);
                SendVbanCommand($"PresetPatch[{commandIndex}].Select");
            }
            RefreshAllControls();
            Properties.Settings.Default.LastSelectedPresetName = preset.Name;
            Properties.Settings.Default.Save();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

        private void OpenSettingsWindow()
        {
            var settingsWindow = new SettingsWindow(_matrixClient) { Owner = this };
            if (settingsWindow.ShowDialog() == true)
            {
                InitializeVbanClient();
                RefreshAllControls();
            }
        }

        private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null) return;
            double masterOffset = e.NewValue;
            foreach (var slider in _matrixChannelSliders.Values)
            {
                if (_sliderEventHandlers.TryGetValue(slider, out var handler)) { slider.ValueChanged -= handler; }
                double baseGain = (double)slider.Tag;
                double newVisualValue = baseGain + masterOffset;
                newVisualValue = Math.Max(slider.Minimum, Math.Min(slider.Maximum, newVisualValue));
                slider.Value = newVisualValue;
                if (handler != null) { slider.ValueChanged += handler; }
            }
            var preset = _presets.FirstOrDefault(p => p.Name == PresetComboBox.SelectedItem.ToString());
            if (preset != null)
            {
                SendVbanCommand($"PresetPatch[{preset.VbanIndex}].Gain = {((int)e.NewValue)}");
            }
        }

        private void MasterMuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null) return;
            var preset = _presets.FirstOrDefault(p => p.Name == PresetComboBox.SelectedItem.ToString());
            if (preset != null)
            {
                string muteValue = ((System.Windows.Controls.Primitives.ToggleButton)sender).IsChecked == true ? "1" : "0";
                SendVbanCommand($"PresetPatch[{preset.VbanIndex}].Mute = {muteValue}");
            }
        }

        private void MasterNudgeDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null) return;
            var preset = _presets.FirstOrDefault(p => p.Name == PresetComboBox.SelectedItem.ToString());
            if (preset != null) SendVbanCommand($"PresetPatch[{preset.VbanIndex}].Gain += -1.0");
        }

        private void MasterNudgeUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null) return;
            var preset = _presets.FirstOrDefault(p => p.Name == PresetComboBox.SelectedItem.ToString());
            if (preset != null) SendVbanCommand($"PresetPatch[{preset.VbanIndex}].Gain += 1.0");
        }

        private void OnDeactivated(object? sender, EventArgs e) => Hide();
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }

        private void VbanClient_OnStateUpdated(VoicemeeterState newState) { /* Placeholder */ }
    }
}