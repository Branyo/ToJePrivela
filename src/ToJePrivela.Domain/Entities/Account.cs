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
    }

    private Account(string name, string passwordHash, bool isAdmin, DateTime createdAt)
    {
        SetName(name);
        PasswordHash = ValidPasswordHash(passwordHash);
        IsAdmin = isAdmin;
        CreatedAt = UtcTime.Normalize(createdAt);
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

    /// <summary>See <see cref="ReservedId"/>.</summary>
    public bool IsReserved => PasswordHash is null;

    public void RecordSignIn(DateTime signedInAt)
    {
        EnsureNotReserved();
        LastSignedInAt = UtcTime.Normalize(signedInAt);
    }

    /// <summary>Replaces the hash, e.g. with a stronger one of the same password or one set from configuration.</summary>
    public void ChangePasswordHash(string passwordHash)
    {
        EnsureNotReserved();
        PasswordHash = ValidPasswordHash(passwordHash);
    }

    public void GrantAdmin()
    {
        EnsureNotReserved();
        IsAdmin = true;
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

    private static string ValidPasswordHash(string passwordHash) =>
        Guard.AgainstInvalidLength(passwordHash, nameof(passwordHash), 1, PasswordHashMaxLength);
}
