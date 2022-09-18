using Eum.Cores.Spotify.Contracts.Enums;

namespace Eum.Cores.Spotify.Contracts.Models;

public sealed record MercuryPacket
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