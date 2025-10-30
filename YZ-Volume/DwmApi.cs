using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

public static class DwmApi
{
    // This is the C# declaration for the native Windows function.
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // This is the specific attribute we want to set.
    // DWMWA_USE_IMMERSIVE_DARK_MODE = 20
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    // This is our helper method that makes the API call easy to use.
    public static bool UseImmersiveDarkMode(Window window, bool enabled)
    {
        // We need the window's handle (HWND) to talk to the DWM.
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        int enabledValue = enabled ? 1 : 0;

        // Call the native function. A result of 0 means success.
        int result = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabledValue, sizeof(int));
        return result == 0;
    }
}