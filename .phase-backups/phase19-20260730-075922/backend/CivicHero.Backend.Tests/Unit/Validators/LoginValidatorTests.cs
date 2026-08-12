using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Validators;
using FluentValidation.TestHelper;

namespace CivicHero.Backend.Tests.Unit.Validators;

public sealed class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Valid_credentials_should_pass_validation()
    {
        var result = _validator.TestValidate(new LoginRequest
        {
            Email = "citizen@example.com",
            Password = "StrongPassword!123"
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_should_fail_validation(string email)
    {
        var result = _validator.TestValidate(new LoginRequest
        {
            Email = email,
            Password = "StrongPassword!123"
        });

        result.ShouldHaveValidationErrorFor(request => request.Email);
    }

    [Fact]
    public void Empty_password_should_fail_validation()
    {
        var result = _validator.TestValidate(new LoginRequest
        {
            Email = "citizen@example.com",
            Password = string.Empty
        });

        result.ShouldHaveValidationErrorFor(request => request.Password);
    }
}
