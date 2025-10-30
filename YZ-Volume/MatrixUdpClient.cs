using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

public class MatrixUdpClient
{
    //public event Action<VoicemeeterState>? OnStateUpdated;
    public event Action<string>? OnTextReplyReceived;

    // The ONE and ONLY UdpClient
    private UdpClient _udpClient;
    private readonly IPEndPoint _sendEndPoint;

    private readonly byte[] _vbanTextHeader;
    private uint _frameCounter = 0;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly string _streamName;

    public MatrixUdpClient(string ipAddress, int port, string streamName)
    {
        _sendEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        _streamName = streamName;

        // --- THE CRITICAL C-CODE INSPIRED FIX ---
        // We create a UDP client and bind it to a RANDOM, OS-assigned port.
        // This is what a client application should do.
        // It does NOT listen on the server's port (6980).
        _udpClient = new UdpClient();
        // We then "connect" it. This tells the OS that this client will primarily
        // be sending packets TO the server's endpoint. This helps the OS route
        // replies from the server BACK to this client's random port.
        _udpClient.Connect(_sendEndPoint);

        _vbanTextHeader = CreateVbanTextHeader();
    }

    public void StartListener()
    {
        if (_cancellationTokenSource != null) return;
        _cancellationTokenSource = new CancellationTokenSource();
        //Task.Run(() => ListenForPackets(_cancellationTokenSource.Token));
        Task.Run(() => SubscriptionManager(_cancellationTokenSource.Token));
    }

    public void StopListener()
    {
        _cancellationTokenSource?.Cancel();
        _udpClient.Close();
    }

    public void SendCommand(string command)
    {
        // The implementation details of SendCommand and SubscribeToRealtimeUpdates are correct.
        // We just need to make sure we use the correctly configured client.
        try
        {
            byte[] frameBytes = BitConverter.GetBytes(_frameCounter);
            if (!BitConverter.IsLittleEndian) Array.Reverse(frameBytes);
            frameBytes.CopyTo(_vbanTextHeader, 24);

            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            byte[] packet = _vbanTextHeader.Concat(commandBytes).ToArray();

            // Because we used "Connect", we can use the simpler "Send" method.
            _udpClient.Send(packet, packet.Length);
            _frameCounter++;
        }
        catch (ObjectDisposedException) { /* Socket was closed, ignore */ }
    }

    private async Task ListenForPackets(CancellationToken token)
    {
        //while (!token.IsCancellationRequested)
        //{
        //    try
        //    {
        //        // We listen for replies on the same client we send from.
        //        var result = await _udpClient.ReceiveAsync(token);
        //        var buffer = result.Buffer;

        //        // The C code checks if the reply came from the IP we are talking to.
        //        if (!result.RemoteEndPoint.Equals(_sendEndPoint)) continue;

        //        if (buffer.Length > 28 && buffer[0] == 'V')
        //        {
        //            byte subProtocol = (byte)(buffer[4] & 0xE0);
        //            if (subProtocol == 0x60) // SERVICE
        //            {
        //                byte serviceFunction = buffer[5];
        //                byte serviceId = buffer[6];

        //                if (serviceId == 33) // RT-Packet
        //                {
        //                    ParseRtPacket(buffer.AsSpan(28));
        //                }
        //                else if (serviceFunction == 0x80 && serviceId == 0x02) // Service Reply
        //                {
        //                    string reply = Encoding.UTF8.GetString(buffer.AsSpan(28));
        //                    OnTextReplyReceived?.Invoke(reply.TrimEnd('\0'));
        //                }
        //            }
        //        }
        //    }
        //    catch (OperationCanceledException) { break; }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"UDP Listen Error: {ex.Message}");
        //    }
        //}
        await Task.CompletedTask;
    }

    // The rest of the file (SubscribeToRealtimeUpdates, ParseRtPacket, etc.) is correct
    // and does not need to be changed. I will include it for completeness.

    private void SubscribeToRealtimeUpdates()
    {
        Debug.WriteLine("Sending VBAN RT-Packet subscription request...");
        var header = new byte[28];
        Encoding.ASCII.GetBytes("VBAN").CopyTo(header, 0);
        header[4] = 0x60;
        header[5] = 0;
        header[6] = 32;
        header[7] = 15;
        Encoding.ASCII.GetBytes(_streamName).CopyTo(header, 8);

        try
        {
            // Because we used "Connect", we can use the simpler "Send" method.
            _udpClient.Send(header, header.Length);
        }
        catch (ObjectDisposedException) { /* ignore */ }
    }

    private async Task SubscriptionManager(CancellationToken token)
    {
        //while (!token.IsCancellationRequested)
        //{
        //    SubscribeToRealtimeUpdates();
        //    try { await Task.Delay(TimeSpan.FromSeconds(10), token); }
        //    catch (TaskCanceledException) { break; }
        //}
        await Task.CompletedTask;
    }

    private byte[] CreateVbanTextHeader()
    {
        var header = new byte[28];
        Encoding.ASCII.GetBytes("VBAN").CopyTo(header, 0);
        header[4] = 0x52;
        header[5] = 0x00;
        header[6] = 0x00;
        header[7] = 0x10;
        Encoding.ASCII.GetBytes(_streamName).CopyTo(header, 8);
        return header;
    }

    private void ParseRtPacket(Span<byte> data)
    {
        //if (data.Length < Marshal.SizeOf<VoicemeeterState>()) return;
        //var handle = GCHandle.Alloc(data.ToArray(), GCHandleType.Pinned);
        //try
        //{
        //    IntPtr ptr = handle.AddrOfPinnedObject();
        //    var state = Marshal.PtrToStructure<VoicemeeterState>(ptr);
        //    OnStateUpdated?.Invoke(state);
        //}
        //finally
        //{
        //    handle.Free();
        //}
    }
}