using System.Runtime.InteropServices;
using System.Text;

// This struct is a C# representation of the T_VBAN_VMRT_PACKET from VoicemeeterRemote.h.
// It must match the memory layout exactly.
// The Pack = 1 attribute is the C# equivalent of C's '#pragma pack(1)', which prevents
// the compiler from adding padding bytes between fields.
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct VoicemeeterState
{
    // --- Header Information ---
    public byte VoicemeeterType;
    public byte Reserved;
    public ushort BufferSize;
    public uint VoicemeeterVersion;
    public uint OptionBits;
    public uint Samplerate;

    // --- Real-time Levels ---
    // Pre-fader input peak levels in dB * 100
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 34)]
    public short[] InputLeveldB100;

    // Bus output peak levels in dB * 100
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public short[] OutputLeveldB100;

    public uint TransportBit;

    // --- Strip and Bus States (Mute, Solo, etc.) ---
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public uint[] StripState;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public uint[] BusState;

    // --- Strip and Bus Gains ---
    // The C struct has 8 layers of strip gains. We only care about the first one,
    // but we must account for the memory space of all 8.
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public short[] StripGaindB100Layer1;

    // This private field reserves the memory space for layers 2 through 8
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8 * 7)]
    private readonly short[] _stripGainLayers2to8;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public short[] BusGaindB100;

    // --- Labels ---
    // The C struct has fixed-size char arrays for UTF-8 strings.
    // We marshal them as raw byte arrays.
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8 * 60)]
    private readonly byte[] _stripLabelBytes;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8 * 60)]
    private readonly byte[] _busLabelBytes;

    // --- Helper Methods to get string labels ---
    // These methods find the correct 60-byte chunk from the raw byte array,
    // convert it from UTF-8 to a C# string, and trim any null characters.
    public string GetStripLabel(int index)
    {
        if (index < 0 || index >= 8) return string.Empty;
        var labelBytes = new byte[60];
        Array.Copy(_stripLabelBytes, index * 60, labelBytes, 0, 60);
        return Encoding.UTF8.GetString(labelBytes).TrimEnd('\0');
    }

    public string GetBusLabel(int index)
    {
        if (index < 0 || index >= 8) return string.Empty;
        var labelBytes = new byte[60];
        Array.Copy(_busLabelBytes, index * 60, labelBytes, 0, 60);
        return Encoding.UTF8.GetString(labelBytes).TrimEnd('\0');
    }
}