using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Identity.Passwords;

/// <summary>
/// ASP.NET Core Identity's hasher: PBKDF2 with HMAC-SHA512, a random 128-bit salt per password and the settings stored
/// in the hash, so hashes made with fewer iterations still verify and are reported for rehashing.
/// </summary>
public sealed class Pbkdf2PasswordHasher : Application.Abstractions.Identity.IPasswordHasher
{
    /// <summary>OWASP's recommendation for PBKDF2-HMAC-SHA512 (Identity's own default is 100 000).</summary>
    public const int IterationCount = 210_000;

    private readonly PasswordHasher<Account> _hasher = new(Options.Create(new PasswordHasherOptions
    {
        CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
        IterationCount = IterationCount
    }));

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return _hasher.HashPassword(null!, password);
    }

    public PasswordCheck Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(password))
        {
            return PasswordCheck.Failed;
        }

        try
        {
            return _hasher.VerifyHashedPassword(null!, passwordHash, password) switch
            {
                PasswordVerificationResult.Success => PasswordCheck.Succeeded,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SucceededRehashNeeded,
                _ => PasswordCheck.Failed
            };
        }
        catch (FormatException)
        {
            // Not a hash this hasher made.
            return PasswordCheck.Failed;
        }
    }
}
