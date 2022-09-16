using AutoFixture;
using Eum.Cores.Apple;
using Eum.Cores.Apple.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AppleCoreTests;

public class AppleCoreConstructorTests
{
    private readonly IFixture _fixture = new Fixture();
    
    [Fact]
    public void InstantiateCoreWithDefaultShouldBeValid()
    {
        var mockConfig = _fixture
            .Build<DeveloperTokenConfiguration>()
            .Create();
        var staticMediaTokenHandler = new StaticMediaTokenHandler(_fixture.Build<string>().Create());

        Action CreateCore = () =>  AppleCore.Create(mockConfig, staticMediaTokenHandler);

        CreateCore.Should().NotThrow();
    } 
}