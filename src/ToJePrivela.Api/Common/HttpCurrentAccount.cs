using System.Globalization;
using System.Security.Claims;
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

    public int Id =>
        _httpContextAccessor.HttpContext?.User is { } user && IdOf(user) is { } id
            ? id
            : throw new InvalidOperationException("The request carries no signed-in account.");

    /// <summary>
    /// The signed-in account's id, or <c>null</c> when the user carries no valid access token (authentication sets the
    /// user only for a token that is still valid).
    /// </summary>
    public static int? IdOf(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && int.TryParse(
            user.FindFirst(AccessTokenClaims.Subject)?.Value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var id)
            ? id
            : null;
}
