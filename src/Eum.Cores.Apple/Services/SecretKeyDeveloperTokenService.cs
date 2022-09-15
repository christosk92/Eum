using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;

namespace Eum.Cores.Apple.Services;

internal sealed class SecretKeyDeveloperTokenService : IDeveloperTokenService
{
    private DeveloperTokenConfiguration _configuration;

    public SecretKeyDeveloperTokenService(IOptions<DeveloperTokenConfiguration> developerTokenOption)
    {
        _configuration = developerTokenOption.Value;
        ArgumentNullException.ThrowIfNull(_configuration.KeyId);
        ArgumentNullException.ThrowIfNull(_configuration.TeamId);
        ArgumentNullException.ThrowIfNull(_configuration.PathToFile);
    }

    private TokenData? _previousToken;
    public ValueTask<TokenData> GetDeveloperTokenAsync(CancellationToken ct = default)
    {
        if (_previousToken is
            {
                HasExpired: false
            })
        {
            return new ValueTask<TokenData>(_previousToken);
        }
        var generatedAt = DateTimeOffset.Now;
        var token = GetToken();
        _previousToken = new TokenData
        {
            TokenValue = token,
            ExpiresAt = generatedAt.AddMinutes(59)
        };
        return new ValueTask<TokenData>(_previousToken);
    }
    private string GetToken()
    {
        var dsa = GetECDsa();
        return CreateJwt(dsa, _configuration.KeyId!, _configuration.TeamId!);
    }

    private ECDsa GetECDsa()
    {
        using TextReader reader = System.IO.File.OpenText(_configuration.PathToFile!);
        // var ecPrivateKeyParameters =
        //     (ECPrivateKeyParameters)new Org.BouncyCastle.OpenSsl.PemReader(reader).ReadObject();
        // var x = ecPrivateKeyParameters.Parameters.G.AffineXCoord.GetEncoded();
        // var y = ecPrivateKeyParameters.Parameters.G.AffineYCoord.GetEncoded();
        // var d = ecPrivateKeyParameters.D.ToByteArrayUnsigned();
        //
        // // Convert the BouncyCastle key to a Native Key.
        // var msEcp = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = { X = x, Y = y }, D = d };
        // return ECDsa.Create(msEcp);
        var ecPrivateKeyParameters =
            (ECPrivateKeyParameters)new Org.BouncyCastle.OpenSsl.PemReader(reader).ReadObject();
        var q = ecPrivateKeyParameters.Parameters.G.Multiply(ecPrivateKeyParameters.D).Normalize();
        var x = q.AffineXCoord.GetEncoded();
        var y = q.AffineYCoord.GetEncoded();
        var d = ecPrivateKeyParameters.D.ToByteArrayUnsigned();
        // Convert the BouncyCastle key to a Native Key.
        var msEcp = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = { X = x, Y = y }, D = d };
        return ECDsa.Create(msEcp);
    }

    private string CreateJwt(ECDsa key, string keyId, string teamId)
    {
        var securityKey = new ECDsaSecurityKey(key) { KeyId = keyId };
        var credentials = new SigningCredentials(securityKey, "ES256");

        var descriptor = new SecurityTokenDescriptor
        {
            IssuedAt = DateTime.Now,
            Issuer = teamId,
            SigningCredentials = credentials,
        };

        var handler = new JwtSecurityTokenHandler();
        var encodedToken = handler.CreateEncodedJwt(descriptor);
        return encodedToken;
    }
}