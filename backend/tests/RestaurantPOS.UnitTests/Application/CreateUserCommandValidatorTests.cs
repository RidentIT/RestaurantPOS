using FluentAssertions;

using RestaurantPOS.Application.Users.Commands.CreateUser;
using RestaurantPOS.Domain.Enums;

using Xunit;

namespace RestaurantPOS.UnitTests.Application;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    private static CreateUserCommand Valid(
        string username = "cashier01",
        string password = "Cashier@2026",
        UserRole role = UserRole.User,
        string? email = null,
        IReadOnlyCollection<AppModule>? modules = null) =>
        new(username, "Ravi Kumar", email, password, role, modules ?? [AppModule.PosBilling]);

    [Fact]
    public void AcceptsAWellFormedCommand()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short1A")]        // under 8 characters
    [InlineData("alllowercase1")]  // no upper-case letter
    [InlineData("ALLUPPERCASE1")]  // no lower-case letter
    [InlineData("NoDigitsHere")]   // no digit
    public void RejectsPasswordsThatFailThePolicy(string password)
    {
        var result = _validator.Validate(Valid(password: password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has spaces")]
    [InlineData("bad!char")]
    public void RejectsMalformedUsernames(string username)
    {
        var result = _validator.Validate(Valid(username: username));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Username));
    }

    [Fact]
    public void RejectsAMalformedEmailButAllowsNone()
    {
        _validator.Validate(Valid(email: "not-an-email")).IsValid.Should().BeFalse();
        _validator.Validate(Valid(email: null)).IsValid.Should().BeTrue();
        _validator.Validate(Valid(email: "ravi@srilakshmi.lk")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(AppModule.UserManagement)]
    [InlineData(AppModule.SystemSettings)]
    public void RejectsGrantingAdministrativeModulesDirectly(AppModule module)
    {
        var result = _validator.Validate(Valid(modules: [module]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Modules));
    }

    [Fact]
    public void RejectsModuleValuesOutsideTheCatalog()
    {
        var result = _validator.Validate(Valid(modules: [(AppModule)999]));

        result.IsValid.Should().BeFalse();
    }
}