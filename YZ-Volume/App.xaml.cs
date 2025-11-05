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
            // --- THIS IS THE FIX ---
            // By creating an instance of MMDeviceEnumerator here, we force NAudio
            // to initialize the COM environment first, in a way that is compatible
            // with both itself and the AudioSwitcher library. We don't need to
            // use the variable; just creating it is enough.
            var _ = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            // --- END OF FIX ---

            base.OnStartup(e);
        }
    }

}
