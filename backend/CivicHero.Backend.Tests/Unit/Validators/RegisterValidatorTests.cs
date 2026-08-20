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
    [InlineData("Abhimanyu123")]
    [InlineData("John_Doe")]
    [InlineData("Jane@Doe")]
    [InlineData("Test!")]
    [InlineData("A.B.")]
    public void Full_name_with_digits_or_special_characters_should_fail(string fullName)
    {
        var request = ValidRequest();
        request.FullName = fullName;

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(item => item.FullName)
            .WithErrorMessage("Full name can contain letters and spaces only. Digits and special characters are not allowed.");
    }

    [Theory]
    [InlineData("Abhimanyu Patil")]
    [InlineData("अभिमन्यु पाटील")]
    [InlineData("Élodie Martin")]
    [InlineData("  Vaishnavi   Deore  ")]
    public void Alphabetic_full_name_should_pass(string fullName)
    {
        var request = ValidRequest();
        request.FullName = fullName;

        _validator.TestValidate(request).ShouldNotHaveValidationErrorFor(item => item.FullName);
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
