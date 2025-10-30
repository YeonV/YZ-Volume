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

            _notifyIcon.MouseClick += (sender, args) =>
            {
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

            if (_matrixClient != null)
            {
                int lastPresetIndex = Properties.Settings.Default.LastSelectedPresetIndex;
                if (lastPresetIndex >= 0 && lastPresetIndex < PresetComboBox.Items.Count)
                {
                    PresetComboBox.SelectionChanged -= PresetComboBox_SelectionChanged;
                    PresetComboBox.SelectedIndex = lastPresetIndex;
                    PresetComboBox.SelectionChanged += PresetComboBox_SelectionChanged;

                    ApplyPreset(lastPresetIndex);
                }
            }
            else
            {
                RefreshAllControls(); // Build UI if VBAN is off
            }
        }

        private void InitializeVbanClient()
        {
            if (Properties.Settings.Default.VbanEnabled)
            {
                PresetComboBox.Items.Clear();
                foreach (var preset in PresetDataManager.Presets)
                {
                    PresetComboBox.Items.Add(preset.Name);
                }
                PresetComboBox.Visibility = Visibility.Visible;

                if (_matrixClient == null)
                {
                    var settings = Properties.Settings.Default;
                    _matrixClient = new MatrixUdpClient(settings.VbanIpAddress, settings.VbanPort, settings.VbanStreamName);
                    //_matrixClient.OnStateUpdated += VbanClient_OnStateUpdated;
                    _matrixClient.StartListener();
                }
            }
            else
            {
                PresetComboBox.Visibility = Visibility.Collapsed;
                if (_matrixClient != null)
                {
                    //_matrixClient.OnStateUpdated -= VbanClient_OnStateUpdated;
                    _matrixClient.StopListener();
                    _matrixClient = null;
                }
            }
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

            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                                     .Union(enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active));

            foreach (var device in devices)
            {
                if (savedDeviceIDs.Contains(device.ID))
                {
                    AddDeviceToUI(device);
                }
            }
        }

        // --- THIS METHOD IS NOW CORRECTLY FILLED ---
        private void AddDeviceToUI(MMDevice device)
        {
            var deviceGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var muteButton = new System.Windows.Controls.Primitives.ToggleButton { Style = (Style)FindResource("MuteToggleButtonStyle"), IsChecked = device.AudioEndpointVolume.Mute, VerticalAlignment = VerticalAlignment.Center };

            string displayName;
            var customNamesJson = Properties.Settings.Default.CustomDeviceNames;
            if (!string.IsNullOrEmpty(customNamesJson))
            {
                var customNamesDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(customNamesJson) ?? new Dictionary<string, string>();
                displayName = customNamesDict.ContainsKey(device.ID) && !string.IsNullOrWhiteSpace(customNamesDict[device.ID]) ? customNamesDict[device.ID] : device.FriendlyName;
            }
            else
            {
                displayName = device.FriendlyName;
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

            if (_matrixClient == null || presetIndex < 0 || presetIndex >= PresetDataManager.Presets.Count)
            {
                MatrixSeparator.Visibility = Visibility.Collapsed;
                return;
            }

            MatrixSeparator.Visibility = Visibility.Visible;
            var preset = PresetDataManager.Presets[presetIndex];
            foreach (var control in preset.Controls)
            {
                AddMatrixControlToUI(control);
            }
        }

        // --- THIS METHOD IS NOW CORRECTLY FILLED ---
        private void AddMatrixControlToUI(MatrixControl control)
        {
            var deviceGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var muteButton = new System.Windows.Controls.Primitives.ToggleButton { Style = (Style)FindResource("MuteToggleButtonStyle"), VerticalAlignment = VerticalAlignment.Center };
            var nameLabel = new TextBlock { Text = control.Label, Foreground = System.Windows.Media.Brushes.WhiteSmoke, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0), TextTrimming = TextTrimming.CharacterEllipsis };
            var volumeSlider = new Slider { Minimum = -100, Maximum = 0, Value = control.InitialGain, Style = (Style)FindResource("UltimateSliderStyle"), IsSnapToTickEnabled = true, TickFrequency = 1 };

            volumeSlider.ValueChanged += (sender, args) =>
            {
                string commandToSend = $"{control.CommandBase}.dBGain = {((int)args.NewValue)}";
                SendVbanCommand(commandToSend);
            };

            muteButton.Click += (sender, args) =>
            {
                string muteValue = muteButton.IsChecked == true ? "1" : "0";
                string commandToSend = $"{control.CommandBase}.Mute = {muteValue}";
                SendVbanCommand(commandToSend);
            };

            Grid.SetColumn(muteButton, 0);
            Grid.SetColumn(nameLabel, 1);
            Grid.SetColumn(volumeSlider, 2);
            deviceGrid.Children.Add(muteButton);
            deviceGrid.Children.Add(nameLabel);
            deviceGrid.Children.Add(volumeSlider);
            MatrixControlsPanel.Children.Add(deviceGrid);
        }

        private void SendVbanCommand(string command)
        {
            _matrixClient?.SendCommand(command);
            System.Diagnostics.Debug.WriteLine($"Sent VBAN Command: {command}");
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || PresetComboBox.SelectedItem == null || _matrixClient == null) return;
            int presetIndex = PresetComboBox.SelectedIndex;
            if (presetIndex == -1) return;

            ApplyPreset(presetIndex);
        }

        private void ApplyPreset(int presetIndex)
        {
            if (presetIndex < 0 || presetIndex >= PresetDataManager.Presets.Count) return;

            int commandIndex = presetIndex + 1;
            SendVbanCommand("Command.ResetGrid");
            System.Threading.Thread.Sleep(50);
            SendVbanCommand($"PresetPatch[{commandIndex}].Apply");
            System.Threading.Thread.Sleep(50);
            SendVbanCommand($"PresetPatch[{commandIndex}].Select");

            RefreshAllControls();

            Properties.Settings.Default.LastSelectedPresetIndex = presetIndex;
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

        // --- Boilerplate and Unused Methods ---
        private void OnDeactivated(object? sender, EventArgs e) => Hide();
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }
        private void VbanClient_OnStateUpdated(VoicemeeterState newState)
        {
            // Placeholder
        }
    }
}