using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ToJePrivela.Identity.Tokens;

/// <summary>The access tokens this API issues after a sign-in and accepts afterwards.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>HMAC-SHA256 wants a key of at least 256 bits.</summary>
    public const int SigningKeyMinLength = 32;

    [Required]
    public string Issuer { get; set; } = "ToJePrivela";

    [Required]
    public string Audience { get; set; } = "ToJePrivela";

    /// <summary>Set through user-secrets or the Authentication__Jwt__SigningKey environment variable, never in source.</summary>
    [Required(ErrorMessage = "Authentication:Jwt:SigningKey must be configured.")]
    [MinLength(SigningKeyMinLength, ErrorMessage = "Authentication:Jwt:SigningKey must have at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>How long one sign-in lasts; 24 hours, so a login made for a game night still works the next day.</summary>
    [Range(5, 43200)]
    public int LifetimeMinutes { get; set; } = 1440;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = CreateSigningKey(),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = AccessTokenClaims.Name,
        RoleClaimType = AccessTokenClaims.Role
    };
}
