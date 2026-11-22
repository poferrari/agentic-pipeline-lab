using FluentAssertions;
using Xunit;

namespace App.IntegrationTests.Infrastructure;

public sealed class InfrastructureFallbackPolicyTests
{
    [Fact]
    public void EnsureRealInfrastructure_WhenContainersFailedAndNoOptIn_ShouldFailLoudlyNamingTheOptInVariable()
    {
        var startupFailure = new InvalidOperationException("Docker endpoint not found");

        var act = () => InfrastructureFallbackPolicy.EnsureRealInfrastructure(startupFailure, optInValue: null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{InfrastructureFallbackPolicy.OptInVariable}*")
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Docker endpoint not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("1")]
    [InlineData("yes")]
    public void EnsureRealInfrastructure_WhenContainersFailedAndOptInIsNotExactlyTrue_ShouldStillFail(string optIn)
    {
        var act = () => InfrastructureFallbackPolicy.EnsureRealInfrastructure(new InvalidOperationException("boom"), optIn);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void EnsureRealInfrastructure_WhenContainersFailedAndOptInIsTrue_ShouldAllowInMemoryFallback(string optIn)
    {
        var act = () => InfrastructureFallbackPolicy.EnsureRealInfrastructure(new InvalidOperationException("boom"), optIn);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureRealInfrastructure_WhenContainersStartedFine_ShouldNotThrow()
    {
        var act = () => InfrastructureFallbackPolicy.EnsureRealInfrastructure(startupFailure: null, optInValue: null);

        act.Should().NotThrow();
    }
}
