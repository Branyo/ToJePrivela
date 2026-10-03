using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts.Dtos;

/// <summary>
/// <see cref="Token"/> is what the provider's browser sign-in returned: a Google ID token (the <c>credential</c>) or a
/// Facebook access token.
/// </summary>
public sealed class SignInRequest
{
    public const int TokenMaxLength = 8192;

    [Required(ErrorMessage = "Provider is required.")]
    [EnumDataType(typeof(IdentityProvider), ErrorMessage = "Provider should be Google or Facebook.")]
    public IdentityProvider? Provider { get; init; }

    [Required(ErrorMessage = "Token is required.")]
    [StringLength(TokenMaxLength, ErrorMessage = "Token is too long.")]
    public string Token { get; init; } = default!;
}
