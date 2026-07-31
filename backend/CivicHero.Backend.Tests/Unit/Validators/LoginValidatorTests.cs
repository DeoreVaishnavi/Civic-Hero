using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Validators;
using FluentValidation.TestHelper;

namespace CivicHero.Backend.Tests.Unit.Validators;

public sealed class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Theory]
    [InlineData("citizen@example.com")]
    [InlineData("+919876543210")]
    public void Email_or_phone_identifier_should_pass_validation(string identifier)
    {
        var result = _validator.TestValidate(new LoginRequest
        {
            Identifier = identifier,
            Password = "StrongPassword!123"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Legacy_email_property_should_remain_compatible()
    {
        var result = _validator.TestValidate(new LoginRequest
        {
            Email = "citizen@example.com",
            Password = "StrongPassword!123"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_identifier_should_fail_validation()
    {
        var result = _validator.TestValidate(new LoginRequest { Password = "StrongPassword!123" });
        result.ShouldHaveValidationErrorFor(request => request.EffectiveIdentifier);
    }

    [Fact]
    public void Empty_password_should_fail_validation()
    {
        var result = _validator.TestValidate(new LoginRequest
        {
            Identifier = "citizen@example.com",
            Password = string.Empty
        });
        result.ShouldHaveValidationErrorFor(request => request.Password);
    }
}
