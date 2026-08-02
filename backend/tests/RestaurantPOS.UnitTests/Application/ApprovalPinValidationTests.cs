using FluentAssertions;

using RestaurantPOS.Application.Authentication.Commands.SetApprovalPin;
using RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;

using Xunit;

namespace RestaurantPOS.UnitTests.Application;

public class ApprovalPinValidationTests
{
    private readonly SetApprovalPinCommandValidator _setValidator = new();
    private readonly VerifyApprovalPinCommandValidator _verifyValidator = new();

    [Fact]
    public void ANullPinIsAllowedOnSet_MeaningGenerateOneForMe()
    {
        _setValidator.Validate(new SetApprovalPinCommand("Current@2026", null)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("123")]     // too short
    [InlineData("12345")]   // too long
    [InlineData("12a4")]    // not all digits
    [InlineData("")]
    public void RejectsPinsThatAreNotFourDigits(string pin)
    {
        _setValidator.Validate(new SetApprovalPinCommand("Current@2026", pin)).IsValid.Should().BeFalse();
        _verifyValidator.Validate(new VerifyApprovalPinCommand(pin, null)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("0000")]
    [InlineData("4821")]
    [InlineData("9999")]
    public void AcceptsAnyFourDigitPin(string pin)
    {
        _setValidator.Validate(new SetApprovalPinCommand("Current@2026", pin)).IsValid.Should().BeTrue();
        _verifyValidator.Validate(new VerifyApprovalPinCommand(pin, null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SettingAPinRequiresTheCurrentPassword()
    {
        _setValidator.Validate(new SetApprovalPinCommand("", "1234")).IsValid.Should().BeFalse();
    }
}