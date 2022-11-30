namespace SpotifyTcp.Models;

public record struct MercuryPacket
{
    public MercuryPacket(MercuryPacketType cmd, byte[] payload)
    {
        Cmd = cmd;
        Payload = payload;
    }


    public static MercuryPacketType ParseType(byte input)
    {
        return (MercuryPacketType)input;
    }

    public MercuryPacketType Cmd { get; }
    public byte[] Payload { get; }
    
    public void Deconstruct(out MercuryPacketType Cmd, out byte[] Payload)
    {
        Cmd = this.Cmd;
        Payload = this.Payload;
    }}