using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Validators;
using FluentValidation.TestHelper;

namespace CivicHero.Backend.Tests.Unit.Validators;

public sealed class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Strong_registration_request_should_pass()
    {
        var request = ValidRequest();
        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("alllowercase123!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NoNumber!")]
    [InlineData("NoSpecial123")]
    public void Weak_password_should_fail(string password)
    {
        var request = ValidRequest();
        request.Password = password;
        request.ConfirmPassword = password;

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(item => item.Password);
    }

    [Fact]
    public void Password_confirmation_must_match()
    {
        var request = ValidRequest();
        request.ConfirmPassword = "DifferentPassword!123";

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(item => item.ConfirmPassword);
    }

    private static RegisterRequest ValidRequest() => new()
    {
        FullName = "Test Citizen",
        Email = "citizen@example.com",
        Phone = "+91 9876543210",
        Password = "StrongPassword!123",
        ConfirmPassword = "StrongPassword!123"
    };
}
