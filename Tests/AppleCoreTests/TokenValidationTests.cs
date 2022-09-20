using System.Net;
using System.Text.Json;
using AutoFixture;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Contracts.Models;
using Eum.Cores.Apple.Contracts.Models.Response;
using Eum.Cores.Apple.Contracts.Models.Response.StoreFront;
using Eum.Cores.Apple.Services;
using FluentAssertions;
using Moq;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace AppleCoreTests;

public sealed class TokenValidationTests
{
    private readonly MockHttpMessageHandler _httpClientHandler;
    private readonly ITokenValidation _sut;
    private readonly IFixture _fixture = new Fixture();
    const string correct_bearer = "true_bearer";
    public TokenValidationTests()
    {
        _httpClientHandler = new MockHttpMessageHandler();
        _sut = new TokenValidation(new HttpClient(_httpClientHandler));
    }

    [Fact]
    public async Task ValidateBearerToken_ShouldReturnTrue_WhenValidTokenPassed()
    {
        const string fake_bearer = "fake_bearer";

        var fakeResponseData = _fixture
            .Build<PaginatedResourceCollectionResponse<StoreFrontObject>>()
            .Create();

        _httpClientHandler.
            When("https://api.music.apple.com/v1/storefronts")
            .WithHeaders("Authorization", $"Bearer {fake_bearer}")
            .Respond("application/json", JsonSerializer.Serialize(fakeResponseData)); // Respond with JSON

        _httpClientHandler.Fallback.Respond(HttpStatusCode.Unauthorized);
 
        var validateToken = await _sut.ValidateDeveloperTokenAsync(fake_bearer);

        validateToken.Should().Be(true);
    }
    
    [Fact]
    public async Task ValidateBearerToken_ShouldReturnFalse_WhenValidTokenPassed()
    {
        const string fake_bearer = "invalid_bearer";
        const string correct_bearer = "true_bearer";

        var fakeResponseData = _fixture
            .Build<PaginatedResourceCollectionResponse<StoreFrontObject>>()
            .Create();

        _httpClientHandler.
            Expect("https://api.music.apple.com/v1/storefronts")
            .WithHeaders("Authorization", $"Bearer {correct_bearer}")
            .Respond("application/json", JsonSerializer.Serialize(fakeResponseData)); // Respond with JSON
  
        _httpClientHandler.Fallback.Respond(HttpStatusCode.Unauthorized);
        
        var validateToken = await _sut.ValidateDeveloperTokenAsync(fake_bearer);

        validateToken.Should().Be(false);
    }
}
