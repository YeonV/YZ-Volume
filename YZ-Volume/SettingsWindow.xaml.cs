using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Needed for MouseButtonEventArgs

using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using Grid = System.Windows.Controls.Grid;

namespace YZ_Volume
{
    public partial class SettingsWindow : Window
    {
        private Dictionary<string, (System.Windows.Controls.CheckBox VisibiltyCheckBox, System.Windows.Controls.TextBox NameTextBox)> deviceControls = new();


        public SettingsWindow()
        {
            InitializeComponent();
            DwmApi.UseImmersiveDarkMode(this, true);
            LoadAllDevices();

            VbanToggleButton.IsChecked = Properties.Settings.Default.VbanEnabled;
            VbanIpTextBox.Text = Properties.Settings.Default.VbanIpAddress;
            VbanPortTextBox.Text = Properties.Settings.Default.VbanPort.ToString();

            VbanToggleButton.Click += (s, e) => UpdateVbanTestPanelVisibility();
            UpdateVbanTestPanelVisibility();
        }
    

        // --- THIS IS THE CORRECT LOCATION FOR THIS METHOD ---
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This allows the user to click and drag the window to move it
            DragMove();
        }
        // ---------------------------------------------------

        private string? GetCurrentPresetString()
        {
            if (PresetComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem)
            {
                int presetIndex = PresetComboBox.SelectedIndex + 1;
                return $"PresetPatch[{presetIndex}]";
            }
            // Return null if no preset is selected
            return null;
        }

        private void PresetComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // This check is important! It prevents the code from running when the
            // window is first created and the ComboBox is being populated.
            // We only want to run this when the USER actually changes the selection.
            if (!this.IsLoaded || PresetComboBox.SelectedItem == null)
            {
                return;
            }

            // The rest of the logic is the same as before
            int presetIndex = PresetComboBox.SelectedIndex + 1;

            string command1 = "Command.ResetGrid";
            string command2 = $"PresetPatch[{presetIndex}].Apply";
            string command3 = $"PresetPatch[{presetIndex}].Select";

            // Send the commands
            SendVbanTestCommand(command1);
            System.Threading.Thread.Sleep(100);
            SendVbanTestCommand(command2);
            System.Threading.Thread.Sleep(100);
            SendVbanTestCommand(command3);

            System.Diagnostics.Debug.WriteLine($"Applied Preset on change: {presetIndex}");
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
                System.Diagnostics.Debug.WriteLine($"Sent VBAN Command: {command}");
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

        private void TestGainUp_Click(object sender, RoutedEventArgs e)
        {
            var preset = GetCurrentPresetString();
            if (preset != null)
            {
                SendVbanTestCommand($"{preset}.Gain += 1.0");
            }
        }

        private void TestGainDown_Click(object sender, RoutedEventArgs e)
        {
            var preset = GetCurrentPresetString();
            if (preset != null)
            {
                SendVbanTestCommand($"{preset}.Gain += -1.0");
            }
        }

        private void TestMuteToggle_Click(object sender, RoutedEventArgs e)
        {
            var preset = GetCurrentPresetString();
            if (preset != null)
            {
                SendVbanTestCommand($"{preset}.Mute = !{preset}.Mute");
            }
        }

        private void SendCustomCommand_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CustomCommandTextBox.Text))
            {
                SendVbanTestCommand(CustomCommandTextBox.Text);
            }
        }

        private void UpdateVbanTestPanelVisibility()
        {
            if (VbanToggleButton.IsChecked == true)
            {
                VbanTestPanel.Visibility = Visibility.Visible;
            }
            else
            {
                VbanTestPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadAllDevices()
        {
            var savedVisibleIDs = Properties.Settings.Default.VisibleDeviceIDs;
            var customNamesJson = Properties.Settings.Default.CustomDeviceNames;

            if (savedVisibleIDs == null) savedVisibleIDs = new System.Collections.Specialized.StringCollection();

            var customNamesDict = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(customNamesJson))
            {
                customNamesDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(customNamesJson) ?? new Dictionary<string, string>();
            }

            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            var allDevices = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.All, NAudio.CoreAudioApi.DeviceState.Active);

            SettingsDeviceListPanel.Children.Clear();
            deviceControls.Clear();

            foreach (var device in allDevices)
            {
                // --- NEW ICON LOGIC STARTS HERE ---

                // 1. Determine which icon to use based on the device type
                string iconCharacter;
                if (device.DataFlow == NAudio.CoreAudioApi.DataFlow.Render)
                {
                    iconCharacter = "\uE767"; // Speaker icon
                }
                else
                {
                    iconCharacter = "\uE720"; // Microphone icon
                }

                // 2. Create a TextBlock for the icon
                var iconTextBlock = new System.Windows.Controls.TextBlock
                {
                    FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
                    Text = iconCharacter,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0) // Add some space between icon and name
                };

                // 3. Create a TextBlock for the name
                var nameTextBlock = new System.Windows.Controls.TextBlock
                {
                    Text = device.FriendlyName,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 4. Create a panel to hold the icon and name together
                var contentPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal
                };
                contentPanel.Children.Add(iconTextBlock);
                contentPanel.Children.Add(nameTextBlock);

                // --- END OF NEW ICON LOGIC ---


                var grid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });

                // 5. Use the new panel as the content for the CheckBox
                var checkBox = new System.Windows.Controls.CheckBox
                {
                    Content = contentPanel, // Use our new panel here!
                    IsChecked = savedVisibleIDs.Contains(device.ID),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = customNamesDict.ContainsKey(device.ID) ? customNamesDict[device.ID] : "",
                    Margin = new Thickness(10, 0, 0, 0),
                    Width = 150,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                };

                System.Windows.Controls.Grid.SetColumn(checkBox, 0);
                System.Windows.Controls.Grid.SetColumn(textBox, 1);
                grid.Children.Add(checkBox);
                grid.Children.Add(textBox);
                SettingsDeviceListPanel.Children.Add(grid);
                deviceControls.Add(device.ID, (checkBox, textBox));
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.VbanEnabled = VbanToggleButton.IsChecked ?? false;
            Properties.Settings.Default.VbanIpAddress = VbanIpTextBox.Text;

            // Safely parse the port number
            if (int.TryParse(VbanPortTextBox.Text, out int port))
            {
                Properties.Settings.Default.VbanPort = port;
            }

            var newVisibleIDs = new System.Collections.Specialized.StringCollection();
            var newCustomNamesDict = new Dictionary<string, string>();

            foreach (var pair in deviceControls)
            {
                if (pair.Value.VisibiltyCheckBox.IsChecked == true)
                {
                    newVisibleIDs.Add(pair.Key);
                }
                if (!string.IsNullOrWhiteSpace(pair.Value.NameTextBox.Text))
                {
                    newCustomNamesDict.Add(pair.Key, pair.Value.NameTextBox.Text);
                }
            }

            // Serialize the dictionary into a JSON string
            string customNamesJson = JsonConvert.SerializeObject(newCustomNamesDict);

            // Save the simple collections and the JSON string
            Properties.Settings.Default.VisibleDeviceIDs = newVisibleIDs;
            Properties.Settings.Default.CustomDeviceNames = customNamesJson;


            Properties.Settings.Default.Save();

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        //private void TestVbanButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Read the current values directly from the textboxes on this window
        //    string ipAddress = VbanIpTextBox.Text;
        //    string portText = VbanPortTextBox.Text;
        //    string command = "PresetPatch[1].Gain += -1";

        //    // Validate the input
        //    if (!int.TryParse(portText, out int port))
        //    {
        //        System.Windows.MessageBox.Show("Invalid Port number.", "VBAN Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    MatrixUdpClient? testClient = null;
        //    try
        //    {
        //        // Create a temporary client just for this test
        //        testClient = new MatrixUdpClient(ipAddress, port, "Command1");
        //        testClient.SendCommand(command);

        //        // Success!
        //        System.Diagnostics.Debug.WriteLine($"SUCCESS: Sent VBAN Test Command: '{command}' to {ipAddress}:{port}");
        //        System.Windows.MessageBox.Show($"Successfully sent command:\n\n{command}", "VBAN Test Success", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Failure!
        //        System.Diagnostics.Debug.WriteLine($"FAILURE: VBAN Test failed. Error: {ex.Message}");
        //        System.Windows.MessageBox.Show($"Failed to send VBAN command.\n\nError: {ex.Message}", "VBAN Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //    finally
        //    {
        //        // IMPORTANT: Always close the temporary client to release network resources
        //        testClient?.StopListener(); // The StopListener method in our client also closes the UDP socket.
        //    }
        //}
    }
}