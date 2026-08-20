using CivicHero.Backend.Infrastructure.Security;

namespace CivicHero.Backend.Tests.Unit.Security;

public sealed class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("+91 98765 43210", "+919876543210")]
    [InlineData("(987) 654-3210", "+919876543210")]
    public void Normalize_should_return_e164_style_number(string input, string expected) =>
        PhoneNumberNormalizer.Normalize(input).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    public void Normalize_should_reject_invalid_values(string input) =>
        FluentActions.Invoking(() => PhoneNumberNormalizer.Normalize(input)).Should().Throw<Exception>();
}
