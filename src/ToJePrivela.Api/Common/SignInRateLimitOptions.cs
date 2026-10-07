using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Api.Common;

/// <summary>
/// Fixed-window limits on signing in and creating logins (see <see cref="SignInRateLimiting"/>), both counted over
/// <see cref="WindowSeconds"/>:
/// <list type="bullet">
/// <item><see cref="PermitLimit"/> per client address and login name, so one login's password cannot be guessed at
/// speed while people sharing one address (a party on one Wi-Fi) do not use up each other's attempts;</item>
/// <item><see cref="AddressPermitLimit"/> attempts per client address whatever the names, so one address cannot try many
/// names, or create many logins, at speed either; successful sign-ins do not count.</item>
/// </list>
/// </summary>
public sealed class SignInRateLimitOptions
{
    public const string SectionName = "RateLimiting:SignIn";
    public const string PolicyName = "sign-in";

    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, 10000)]
    public int AddressPermitLimit { get; set; } = 60;

    [Range(1, 86400)]
    public int WindowSeconds { get; set; } = 60;
}
