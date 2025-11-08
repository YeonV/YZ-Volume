using System.Runtime.InteropServices;

public static class InputHelper
{
    // Import the native Windows function
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    // Define the key codes we need
    private const byte VK_LCONTROL = 0xA2;
    private const byte VK_LWIN = 0x5B;
    private const byte VK_V = 0x56;

    // Define the key event flags
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static void SendCtrlWinV()
    {
        // Press the keys down in order
        keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(VK_V, 0, KEYEVENTF_KEYDOWN, 0);

        // Release the keys in reverse order
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, 0);
    }
}