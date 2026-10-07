using System.Diagnostics.CodeAnalysis;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

/// <summary>
/// A login: a name unique among all logins and the hash of its password (the domain never sees the password itself).
/// Logins are never deleted.
/// </summary>
public class Account
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 30;
    public const int PasswordHashMaxLength = 256;
    public const int SecurityStampLength = 32;

    /// <summary>How many players one login may keep.</summary>
    public const int MaxPlayers = 100;

    /// <summary>
    /// Created by the migration that introduced logins, to own everything stored before anyone could sign in. It has
    /// no name and no password (<see cref="IsReserved"/>), so nobody can sign in to it, until
    /// <see cref="TakeOverReserved"/> hands it to the first seeded admin.
    /// </summary>
    public const int ReservedId = 1;

    /// <summary>The form a name is stored in; <see cref="NameKey"/> is what makes two names the same.</summary>
    public static string NormalizeName(string name) => name.Trim();

    private Account()
    {
        Name = string.Empty;
        NameKey = string.Empty;
        SecurityStamp = string.Empty;
    }

    private Account(string name, string passwordHash, bool isAdmin, DateTime createdAt)
    {
        SetName(name);
        PasswordHash = ValidPasswordHash(passwordHash);
        IsAdmin = isAdmin;
        CreatedAt = UtcTime.Normalize(createdAt);
        SecurityStamp = NewSecurityStamp();
    }

    /// <summary>A login someone created for themselves; never an admin.</summary>
    public static Account Register(string name, string passwordHash, DateTime createdAt) =>
        new(name, passwordHash, isAdmin: false, createdAt);

    /// <summary>An admin login seeded from configuration.</summary>
    public static Account CreateAdmin(string name, string passwordHash, DateTime createdAt) =>
        new(name, passwordHash, isAdmin: true, createdAt);

    public int Id { get; private set; }

    public string Name { get; private set; }

    /// <summary><see cref="Name"/> as <see cref="NameKeys.Of"/> compares it; unique among all logins.</summary>
    public string NameKey { get; private set; }

    /// <summary>Null only while the account <see cref="IsReserved"/>.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Admins manage the shared questions and categories, AI generation included.</summary>
    public bool IsAdmin { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? LastSignedInAt { get; private set; }

    /// <summary>
    /// Changes whenever who may act as this login changes: a password set anew, admin rights granted, the reserved
    /// account taken over. Every access token carries the stamp it was issued with and is refused once it differs, so
    /// such a change ends every sign-in made before it. That is what keeps someone who created a login under an admin's
    /// name before it was configured from going on as admin with the token they already hold.
    /// </summary>
    public string SecurityStamp { get; private set; }

    /// <summary>See <see cref="ReservedId"/>.</summary>
    public bool IsReserved => PasswordHash is null;

    public void RecordSignIn(DateTime signedInAt)
    {
        EnsureNotReserved();
        LastSignedInAt = UtcTime.Normalize(signedInAt);
    }

    /// <summary>Sets a new password's hash (e.g. one set from configuration) and ends every earlier sign-in.</summary>
    public void ChangePasswordHash(string passwordHash)
    {
        EnsureNotReserved();
        PasswordHash = ValidPasswordHash(passwordHash);
        SecurityStamp = NewSecurityStamp();
    }

    /// <summary>A stronger hash of the <em>same</em> password; whoever is signed in stays signed in.</summary>
    public void UpgradePasswordHash(string passwordHash)
    {
        EnsureNotReserved();
        PasswordHash = ValidPasswordHash(passwordHash);
    }

    /// <summary>Making a login admin ends its earlier sign-ins, which may belong to whoever held it before.</summary>
    public void GrantAdmin()
    {
        EnsureNotReserved();

        if (!IsAdmin)
        {
            IsAdmin = true;
            SecurityStamp = NewSecurityStamp();
        }
    }

    public void RevokeAdmin() => IsAdmin = false;

    /// <summary>Hands the reserved account (and so everything stored before logins existed) to a seeded admin.</summary>
    public void TakeOverReserved(string name, string passwordHash, DateTime takenAt)
    {
        if (!IsReserved)
        {
            throw new DomainException("Only the reserved account can be taken over.");
        }

        SetName(name);
        PasswordHash = ValidPasswordHash(passwordHash);
        IsAdmin = true;
        CreatedAt = UtcTime.Normalize(takenAt);
        SecurityStamp = NewSecurityStamp();
    }

    [MemberNotNull(nameof(Name), nameof(NameKey))]
    private void SetName(string name)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
        NameKey = NameKeys.Of(Name);
    }

    private void EnsureNotReserved()
    {
        if (IsReserved)
        {
            throw new DomainException("The reserved account must be taken over by an admin first.");
        }
    }

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

    private static string ValidPasswordHash(string passwordHash) =>
        Guard.AgainstInvalidLength(passwordHash, nameof(passwordHash), 1, PasswordHashMaxLength);
}
