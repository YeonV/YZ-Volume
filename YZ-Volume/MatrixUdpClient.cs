using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MatrixUdpClient
{
    //public event Action<string>? OnTextReplyReceived;

    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _sendEndPoint;

    private readonly byte[] _vbanTextHeader;
    private uint _frameCounter = 0;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly string _streamName;

    public MatrixUdpClient(string ipAddress, int port, string streamName)
    {
        _sendEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        _streamName = streamName;

        // Create the client but DO NOT bind or connect it here.
        _udpClient = new UdpClient();

        _vbanTextHeader = CreateVbanTextHeader();
    }

    public void StartListener()
    {
        if (_cancellationTokenSource != null) return;
        _cancellationTokenSource = new CancellationTokenSource();
        // The listener task will now handle its own UdpClient
        Task.Run(() => ListenForPackets(_cancellationTokenSource.Token));
    }

    public void StopListener()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void SendCommand(string command)
    {
        try
        {
            byte[] frameBytes = BitConverter.GetBytes(_frameCounter);
            frameBytes.CopyTo(_vbanTextHeader, 24);
            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            byte[] packet = _vbanTextHeader.Concat(commandBytes).ToArray();

            // Send to the specific endpoint
            _udpClient.Send(packet, packet.Length, _sendEndPoint);
        }
        catch (ObjectDisposedException) { /* Socket was closed, ignore */ }
    }

    private async Task ListenForPackets(CancellationToken token)
    {
        // The listener creates its OWN client, configures it for reuse, and binds it.
        // This is the most robust model for co-existing with the server.
        using (var listenerClient = new UdpClient())
        {
            listenerClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listenerClient.Client.Bind(new IPEndPoint(IPAddress.Any, _sendEndPoint.Port));

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await listenerClient.ReceiveAsync(token);
                    var buffer = result.Buffer;

                    // Log EVERYTHING, regardless of source, for this test.
                    Debug.WriteLine($"--- PACKET RECEIVED from {result.RemoteEndPoint.Address} ---");
                    Debug.WriteLine($"Packet Length: {buffer.Length} bytes");
                    if (buffer.Length >= 4 && buffer[0] == 'V' && buffer[1] == 'B' && buffer[2] == 'A' && buffer[3] == 'N')
                    {
                        if (buffer.Length >= 28)
                        {
                            string payload = Encoding.UTF8.GetString(buffer.AsSpan(28)).TrimEnd('\0');
                            Debug.WriteLine($"Payload Text: {payload}");
                            string headerHex = BitConverter.ToString(buffer.Take(28).ToArray());
                            Debug.WriteLine($"Header (Hex): {headerHex}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Packet is not a VBAN packet.");
                    }
                    Debug.WriteLine("------------------------------------");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UDP Listen Error: {ex.Message}");
                }
            }
        }
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
}