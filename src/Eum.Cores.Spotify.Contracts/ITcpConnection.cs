namespace Eum.Cores.Spotify.Contracts;

public interface ITcpConnection : IDisposable
{
    bool IsConnected { get; }
    bool DataAvailable { get; }
    int ReadTimeout { get; set; }

    void WriteByte(byte value);
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    Task FlushAsync(CancellationToken ct);
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

    Task<byte[]> ReadCompleteAsync(
        CancellationToken ct = default);
}