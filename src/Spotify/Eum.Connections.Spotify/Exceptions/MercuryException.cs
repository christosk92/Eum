using System;
using System.Text;

namespace Eum.Connections.Spotify.Exceptions;

public sealed class MercuryException : Exception
{
    public MercuryException(Memory<byte>? response, int statusCode) : base(
        $"Mercury returned a bad statuscode of value: {statusCode}. Read exception for more details.")
    {
        StatusCode = statusCode;
        Response = response ?? new Memory<byte>();
    }
    public int StatusCode { get; }
    public Memory<byte> Response { get; }
    public string ResponseAsStr => Encoding.UTF8.GetString(Response.ToArray());

    public override string ToString()
    {
        return $"Mercury returned a bad statuscode of value: {StatusCode}. Response: {ResponseAsStr}.";
    }
}