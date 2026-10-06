using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Identity.Passwords;

namespace ToJePrivela.Identity.Tests.Passwords;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Hash_NeverContainsThePassword()
    {
        var hash = _sut.Hash("correct horse battery staple");

        Assert.DoesNotContain("correct horse", hash, StringComparison.OrdinalIgnoreCase);
        Assert.True(hash.Length <= Account.PasswordHashMaxLength);
    }

    [Fact]
    public void Hash_IsSaltedSoTheSamePasswordHashesDifferently()
    {
        Assert.NotEqual(_sut.Hash("secret-password"), _sut.Hash("secret-password"));
    }

    [Fact]
    public void Verify_AcceptsTheRightPasswordOnly()
    {
        var hash = _sut.Hash("secret-password");

        Assert.Equal(PasswordCheck.Succeeded, _sut.Verify(hash, "secret-password"));
        Assert.Equal(PasswordCheck.Failed, _sut.Verify(hash, "Secret-password"));
        Assert.Equal(PasswordCheck.Failed, _sut.Verify(hash, ""));
    }

    [Fact]
    public void Verify_AsksToRehashAHashMadeWithFewerIterations()
    {
        var weaker = new PasswordHasher<Account>(Options.Create(new PasswordHasherOptions { IterationCount = 10_000 }));
        var hash = weaker.HashPassword(null!, "secret-password");

        Assert.Equal(PasswordCheck.SucceededRehashNeeded, _sut.Verify(hash, "secret-password"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a hash")]
    [InlineData("AQAAAA==")]
    public void Verify_FailsForSomethingThatIsNoHash(string hash)
    {
        Assert.Equal(PasswordCheck.Failed, _sut.Verify(hash, "secret-password"));
    }
}
