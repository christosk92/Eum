using AutoFixture;
using Eum.Cores.Apple;
using Eum.Cores.Apple.Contracts;
using Eum.Cores.Apple.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AppleCoreTests;

public sealed class AppleCoreDITests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IDeveloperTokenService _developerTokenService = Substitute.For<IDeveloperTokenService>();
    [Fact]
    public void Inject_In_DI_Container_Should_Return_Valid_Instance()
    {
        var serviceCollection = new ServiceCollection();
        var fakeConfig = _fixture
            .Build<DeveloperTokenConfiguration>()
            .Create();
        var fakeMediaToken = _fixture
            .Build<string>()
            .Create();

        var newCollection = serviceCollection.AddAppleMusicCore(fakeConfig)
            .WithMediaToken(fakeMediaToken)
            .BuildServiceProvider();

        var getAppleMusicCore =
            newCollection.GetRequiredService<IAppleCore>();

        getAppleMusicCore
            .Should()
            .BeOfType<AppleCore>();
    }
}