using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Identity.Tokens;

namespace ToJePrivela.Identity.Tests.Tokens;

public class JwtAccessTokenIssuerTests
{
    private static readonly JwtOptions Settings = new()
    {
        SigningKey = "unit-tests-signing-key-0123456789abcdef",
        LifetimeMinutes = 60
    };

    [Fact]
    public async Task Issue_SignsATokenTheValidationParametersAccept()
    {
        var token = Issuer().Issue(AccountWithId(7, isAdmin: false));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, Settings.CreateValidationParameters());

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal("7", result.ClaimsIdentity.FindFirst(AccessTokenClaims.Subject)!.Value);
        Assert.Equal("Brano", result.ClaimsIdentity.Name);
        Assert.False(result.ClaimsIdentity.HasClaim(AccessTokenClaims.Role, AccessTokenClaims.AdminRole));
    }

    [Fact]
    public async Task Issue_LeavesAdminRightsOutOfTheToken()
    {
        // The host reads them from the stored login on every request, so revoking them counts at once.
        var token = Issuer().Issue(AccountWithId(1, isAdmin: true));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, Settings.CreateValidationParameters());

        Assert.False(result.ClaimsIdentity.HasClaim(c => c.Type == AccessTokenClaims.Role));
    }

    [Fact]
    public void Issue_ExpiresAfterTheConfiguredLifetime()
    {
        var now = DateTimeOffset.UtcNow;

        var token = Issuer(now).Issue(AccountWithId(7, isAdmin: false));

        Assert.Equal(now.UtcDateTime.AddMinutes(60), token.ExpiresAt);
    }

    [Fact]
    public async Task ValidationParameters_RejectATokenSignedWithAnotherKey()
    {
        var foreign = new JwtAccessTokenIssuer(
            Options.Create(new JwtOptions { SigningKey = new string('x', 40) }),
            TimeProvider.System);

        var token = foreign.Issue(AccountWithId(7, isAdmin: true));
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, Settings.CreateValidationParameters());

        Assert.False(result.IsValid);
    }

    private static JwtAccessTokenIssuer Issuer(DateTimeOffset? now = null) =>
        new(Options.Create(Settings), new FixedTimeProvider(now ?? DateTimeOffset.UtcNow));

    private static Account AccountWithId(int id, bool isAdmin)
    {
        var account = isAdmin
            ? Account.CreateAdmin("Brano", "hash", DateTime.UtcNow)
            : Account.Register("Brano", "hash", DateTime.UtcNow);
        typeof(Account).GetProperty(nameof(Account.Id), BindingFlags.Public | BindingFlags.Instance)!.SetValue(account, id);

        return account;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
