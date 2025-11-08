using System.Configuration;
using System.Data;
using System.Windows;

namespace YZ_Volume
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Prime the COM environment for NAudio and AudioSwitcher
            var _ = new NAudio.CoreAudioApi.MMDeviceEnumerator();            

            base.OnStartup(e);
            var mainWindow = new MainWindow();
        }
    }

}
