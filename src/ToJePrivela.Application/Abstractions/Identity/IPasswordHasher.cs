namespace ToJePrivela.Application.Abstractions.Identity;

/// <summary>One-way, salted and deliberately slow hashing of login passwords; a password is never stored.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordCheck Verify(string passwordHash, string password);
}

public enum PasswordCheck
{
    Failed,
    Succeeded,

    /// <summary>The password is right, but the hash uses older settings and should be replaced by a fresh one.</summary>
    SucceededRehashNeeded
}
