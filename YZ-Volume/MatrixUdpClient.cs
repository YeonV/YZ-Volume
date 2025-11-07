using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MatrixUdpClient
{
    public event Action<string>? OnTextReplyReceived;
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _sendEndPoint;
    private readonly byte[] _vbanTextHeader;
    private long _frameCounter = 0;
    private CancellationTokenSource? _cancellationTokenSource;

    public MatrixUdpClient(string ipAddress, int port, string streamName)
    {
        _sendEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        _vbanTextHeader = CreateVbanTextHeader(streamName);
    }

    public void StartListener()
    {
        if (_cancellationTokenSource != null) return;
        _cancellationTokenSource = new CancellationTokenSource();
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
            long currentFrame = Interlocked.Increment(ref _frameCounter);
            byte[] frameBytes = BitConverter.GetBytes((uint)currentFrame);
            if (!BitConverter.IsLittleEndian) Array.Reverse(frameBytes);
            frameBytes.CopyTo(_vbanTextHeader, 24);
            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            byte[] packet = _vbanTextHeader.Concat(commandBytes).ToArray();
            _udpClient.Send(packet, packet.Length, _sendEndPoint);
        }
        catch (ObjectDisposedException) { /* Ignore */ }
    }

    private async Task ListenForPackets(CancellationToken token)
    {
        using (_udpClient)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(token);
                    var buffer = result.Buffer;
                    if (buffer.Length >= 28 && buffer[0] == 'V' && result.RemoteEndPoint.Equals(_sendEndPoint))
                    {
                        byte protocol = (byte)(buffer[4] & 0xE0);
                        if (protocol == 0x60) // VBAN_PROTOCOL_SERVICE
                        {
                            byte function = buffer[5];
                            byte id = buffer[6];
                            if (function == 0x80 && id == 0x02) // VBAN_SERVICE_FNCT_REPLY
                            {
                                string reply = Encoding.UTF8.GetString(buffer.AsSpan(28));
                                OnTextReplyReceived?.Invoke(reply.TrimEnd('\0'));
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Debug.WriteLine($"UDP Listen Error: {ex.Message}"); }
            }
        }
    }

    private byte[] CreateVbanTextHeader(string streamName)
    {
        var header = new byte[28];
        Encoding.ASCII.GetBytes("VBAN").CopyTo(header, 0);
        header[4] = 0x52;
        header[5] = 0x00;
        header[6] = 0x00;
        header[7] = 0x10;
        Encoding.ASCII.GetBytes(streamName).CopyTo(header, 8);
        return header;
    }
}