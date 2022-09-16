using System.Net.Sockets;
using Eum.Cores.Spotify.Contracts;
using Eum.Cores.Spotify.Helpers;

namespace Eum.Cores.Spotify.Connection;

internal sealed class RawTcpConnection : ITcpConnection
{  
    private readonly TcpClient tcpClient;

    public RawTcpConnection(TcpClient tcpClient)
    {
        this.tcpClient = tcpClient;
    }

    public bool IsConnected {
        get
        {
            try
            {
                return tcpClient is
                {
                    Connected: true
                } && tcpClient.GetStream().ReadTimeout > -2;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public bool DataAvailable => tcpClient.GetStream().DataAvailable;

    public int ReadTimeout
    {
        get => tcpClient.GetStream().ReadTimeout;
        set => tcpClient.GetStream().ReadTimeout = value;
    }

    public void WriteByte(byte value)
    {
        tcpClient.GetStream().WriteByte(value);
    }

    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return tcpClient.GetStream().WriteAsync(buffer, offset, count, cancellationToken);
    }

    public Task FlushAsync(CancellationToken ct)
    {
        return tcpClient.GetStream().FlushAsync(ct);
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return tcpClient.GetStream().ReadAsync(buffer, offset, count, cancellationToken);
    }

    public Task<byte[]> ReadCompleteAsync(CancellationToken ct = default)
    {
        return tcpClient.GetStream().ReadCompleteAsync(ct);
    }

    public void Dispose()
    {
        tcpClient.Dispose();
    }
}