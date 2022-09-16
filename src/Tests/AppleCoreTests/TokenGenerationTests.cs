using System.Text;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Models;
using Eum.Cores.Apple.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AppleCoreTests;

public class TokenGenerationTests
{
    private IDeveloperTokenService _sut;
    private readonly ISecretTokenFileProvider _fileProvider = Substitute.For<ISecretTokenFileProvider>();
    private readonly ITokenValidation _tokenValidation = Substitute.For<ITokenValidation>(); 
    public TokenGenerationTests()
    {
    }
    
    [Fact]
    public async Task GenerateToken_WithValidData_ShouldReturnRightToken()
    {
        const string keyid = "4UCC8666F2";
        const string teamid = "QF7THUQ8VL";
        const string pathtofile = "3";
        const string fakeKey =  "-----BEGIN PRIVATE KEY-----MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgGfVyJqLR/+7FiPYsKLVTDDRfd4jhoS91cXbKwKLyf7+gCgYIKoZIzj0DAQehRANCAAQvoAojXdnBnumyFOrzb+/gHVVa1QKw2ShpkRDHZXrQ8klo+gsQYqYeWxXkPiT8ZCrc4+s7jL3vIcYmku9TOluE-----END PRIVATE KEY-----";

        var correctOptions = new DeveloperTokenConfiguration
        {
            KeyId = keyid,
            TeamId = teamid,
            PathToFile = pathtofile
        };

        _fileProvider.GetTextReaderForFile(pathtofile).Returns(_ =>
        {
            var textToStream = new MemoryStream(Encoding.UTF8.GetBytes(fakeKey));
            textToStream.Position = 0;
            return new StreamReader(textToStream);
        });
        
        _sut = new SecretKeyDeveloperTokenService(
            new OptionsWrapper<DeveloperTokenConfiguration>(correctOptions), _fileProvider);
        var generateToken = await _sut.GetDeveloperTokenAsync(CancellationToken.None);

        generateToken.HasExpired.Should().Be(false);
    }
    
    [Fact]
    public async Task GenerateToken_WithInvalidData_ShouldReturnWrongToken()
    {
        const string keyid = "invalid_key_id";
        const string teamid = "invalid_team_id";
        const string pathtofile = "3";
        const string fakeKey = "invalid_rsa_key";

        var correctOptions = new DeveloperTokenConfiguration
        {
            KeyId = keyid,
            TeamId = teamid,
            PathToFile = pathtofile
        };

        _fileProvider.GetTextReaderForFile(pathtofile).Returns(_ =>
        {
            var textToStream = new MemoryStream(Encoding.UTF8.GetBytes(fakeKey));
            textToStream.Position = 0;
            return new StreamReader(textToStream);
        });
        
        _sut = new SecretKeyDeveloperTokenService(
            new OptionsWrapper<DeveloperTokenConfiguration>(correctOptions), _fileProvider);
        Func<Task> generateToken = async () => { await _sut.GetDeveloperTokenAsync(); };

        await generateToken.Should().ThrowAsync<InvalidCertificateException>();
    }
}