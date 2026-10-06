using System.Globalization;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Identity.Tokens;

namespace ToJePrivela.Api.Common;

/// <summary>Reads the account from the request's validated access token.</summary>
public sealed class HttpCurrentAccount : ICurrentAccount
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentAccount(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int Id
    {
        get
        {
            var subject = _httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaims.Subject)?.Value;

            return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id
                : throw new InvalidOperationException("The request carries no signed-in account.");
        }
    }
}
