using App.Application;
using App.Domain.Common;
using App.Infrastructure;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace App.ArchTests;

public sealed class HexagonalArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Outer_Layers_Or_Frameworks()
    {
        var domainAssembly = typeof(AggregateRoot<>).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "App.Application",
                "App.Infrastructure",
                "App.Api",
                "Microsoft.EntityFrameworkCore",
                "MassTransit",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain layer must remain 100% pure C# with zero dependencies on Application, Infrastructure, or Web frameworks.");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure_Or_Api()
    {
        var applicationAssembly = typeof(App.Application.DependencyInjection).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "App.Infrastructure",
                "App.Api",
                "Microsoft.EntityFrameworkCore",
                "MassTransit",
                "Microsoft.AspNetCore.Http")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application layer must only depend on Domain and abstractions (Ports), never on Driven/Driving Adapters.");
    }

    [Fact]
    public void Infrastructure_Should_Not_Have_Dependency_On_Api()
    {
        var infrastructureAssembly = typeof(App.Infrastructure.DependencyInjection).Assembly;

        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("App.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure adapters must not depend on the API entry point.");
    }
}
