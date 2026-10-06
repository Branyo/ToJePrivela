using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Identity.Tokens;

/// <summary>
/// Signs a JWT naming the account (see <see cref="AccessTokenClaims"/>). It says nothing about admin rights: the host
/// reads those from the stored account on every request, so revoking them takes effect at once.
/// </summary>
public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public AccessToken Issue(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.LifetimeMinutes);

        var claims = new Dictionary<string, object>
        {
            [AccessTokenClaims.Subject] = account.Id.ToString(CultureInfo.InvariantCulture),
            [AccessTokenClaims.Name] = account.Name
        };

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        });

        return new AccessToken(token, expiresAt);
    }
}
