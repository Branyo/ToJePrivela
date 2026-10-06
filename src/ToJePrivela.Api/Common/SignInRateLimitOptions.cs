using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Api.Common;

/// <summary>
/// Fixed-window limit per client address and login name on signing in and creating logins, so passwords cannot be
/// guessed at speed while people sharing one address (a party on one Wi-Fi) do not use up each other's attempts. See
/// <see cref="SignInRateLimiting"/>.
/// </summary>
public sealed class SignInRateLimitOptions
{
    public const string SectionName = "RateLimiting:SignIn";
    public const string PolicyName = "sign-in";

    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, 86400)]
    public int WindowSeconds { get; set; } = 60;
}
