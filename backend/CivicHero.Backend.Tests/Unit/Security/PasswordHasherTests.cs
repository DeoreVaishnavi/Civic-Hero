using CivicHero.Backend.Infrastructure.Security;

namespace CivicHero.Backend.Tests.Unit.Security;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_should_not_store_plain_text_and_should_verify()
    {
        const string password = "StrongPassword!123";
        var hash = _hasher.Hash(password);

        hash.Should().NotBe(password);
        _hasher.Verify(password, hash).Should().BeTrue();
        _hasher.Verify("WrongPassword!123", hash).Should().BeFalse();
    }

    [Fact]
    public void Malformed_hash_should_be_rejected_without_throwing()
    {
        var action = () => _hasher.Verify("StrongPassword!123", "not-a-bcrypt-hash");
        action.Should().NotThrow();
        action().Should().BeFalse();
    }
}
