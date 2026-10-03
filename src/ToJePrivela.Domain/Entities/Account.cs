using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

/// <summary>
/// A login: one identity at one <see cref="IdentityProvider"/>. An account is either <em>bound</em> (it has signed in,
/// so <see cref="ExternalId"/> names the identity) or <em>provisioned</em> (an admin known only by email, bound at the
/// first sign-in with that email). Accounts are never deleted.
/// </summary>
public class Account
{
    public const int ExternalIdMaxLength = 128;
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;

    /// <summary>
    /// Created by the migration that introduced accounts, to own everything stored before anyone could sign in. It
    /// has no identity and no email (<see cref="IsReserved"/>) until <see cref="ProvisionFor"/> hands it to the first
    /// seeded admin.
    /// </summary>
    public const int ReservedId = 1;

    /// <summary>Emails are compared case-insensitively, so they are stored in this form.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private Account()
    {
        DisplayName = string.Empty;
    }

    private Account(IdentityProvider provider, string? externalId, string? email, string displayName, bool isAdmin)
    {
        if (!Enum.IsDefined(provider))
        {
            throw new DomainException($"Unknown identity provider {provider}.");
        }

        Provider = provider;
        ExternalId = externalId is null
            ? null
            : Guard.AgainstInvalidLength(externalId, nameof(externalId), 1, ExternalIdMaxLength);
        Email = ValidEmail(email);
        DisplayName = ValidDisplayName(displayName, Email);
        IsAdmin = isAdmin;
    }

    /// <summary>A login seen for the first time; never an admin.</summary>
    public static Account Register(
        IdentityProvider provider,
        string externalId,
        string? email,
        string displayName,
        DateTime signedInAt)
    {
        var account = new Account(provider, externalId, email, displayName, isAdmin: false);
        account.LastSignedInAt = UtcTime.Normalize(signedInAt);
        return account;
    }

    /// <summary>An admin known by email before ever signing in; the first sign-in with that email binds it.</summary>
    public static Account ProvisionAdmin(IdentityProvider provider, string email)
    {
        var normalized = Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        return new Account(provider, externalId: null, normalized, normalized, isAdmin: true);
    }

    public int Id { get; private set; }

    public IdentityProvider Provider { get; private set; }

    /// <summary>The provider's id of the identity (Google's <c>sub</c>, Facebook's user id); null until bound.</summary>
    public string? ExternalId { get; private set; }

    /// <summary>Verified email, in <see cref="NormalizeEmail"/> form; refreshed on every sign-in.</summary>
    public string? Email { get; private set; }

    public string DisplayName { get; private set; }

    /// <summary>Admins manage the shared questions and categories, AI generation included.</summary>
    public bool IsAdmin { get; private set; }

    public DateTime? LastSignedInAt { get; private set; }

    public bool IsBound => ExternalId is not null;

    /// <summary>See <see cref="ReservedId"/>.</summary>
    public bool IsReserved => !IsBound && Email is null;

    /// <summary>
    /// Records a successful sign-in with the identity. A provisioned account becomes bound to it; a bound account only
    /// ever accepts its own identity.
    /// </summary>
    public void SignIn(string externalId, string? email, string displayName, DateTime signedInAt)
    {
        var id = Guard.AgainstInvalidLength(externalId, nameof(externalId), 1, ExternalIdMaxLength);

        if (IsBound && ExternalId != id)
        {
            throw new DomainException("The account belongs to another identity.");
        }

        if (IsReserved)
        {
            throw new DomainException("The reserved account must be provisioned for an admin first.");
        }

        ExternalId = id;
        Email = ValidEmail(email) ?? Email;
        DisplayName = ValidDisplayName(displayName, Email);
        LastSignedInAt = UtcTime.Normalize(signedInAt);
    }

    public void GrantAdmin() => IsAdmin = true;

    /// <summary>Hands the reserved account (and so everything stored before sign-in) to an admin known by email.</summary>
    public void ProvisionFor(IdentityProvider provider, string email)
    {
        if (!IsReserved)
        {
            throw new DomainException("Only the reserved account can be provisioned for an admin.");
        }

        if (!Enum.IsDefined(provider))
        {
            throw new DomainException($"Unknown identity provider {provider}.");
        }

        Provider = provider;
        Email = ValidEmail(Guard.AgainstNullOrWhiteSpace(email, nameof(email)));
        DisplayName = Email!;
        IsAdmin = true;
    }

    private static string? ValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = NormalizeEmail(email);

        if (normalized.Length > EmailMaxLength || !normalized.Contains('@'))
        {
            throw new DomainException($"email must be an address of at most {EmailMaxLength} characters.");
        }

        return normalized;
    }

    /// <summary>Providers may withhold the name; the email (or a fixed word) stands in, cut to the column size.</summary>
    private static string ValidDisplayName(string? displayName, string? email)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? email ?? "Player" : displayName.Trim();
        return name.Length > DisplayNameMaxLength ? name[..DisplayNameMaxLength] : name;
    }
}
