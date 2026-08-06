using FluentAssertions;

using NetArchTest.Rules;

using Xunit;

namespace RestaurantPOS.ArchitectureTests;

public class LayerDependencyTests
{
    private const string DomainNamespace = "RestaurantPOS.Domain";
    private const string ApplicationNamespace = "RestaurantPOS.Application";
    private const string InfrastructureNamespace = "RestaurantPOS.Infrastructure";

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Or_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Domain.AssemblyReference).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Application.AssemblyReference).Assembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}