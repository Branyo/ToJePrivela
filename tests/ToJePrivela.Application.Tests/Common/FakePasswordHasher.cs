using ToJePrivela.Application.Abstractions.Identity;

namespace ToJePrivela.Application.Tests.Common;

/// <summary>A readable stand-in: the "hash" of <c>secret</c> is <c>hashed:secret</c>; <c>old:secret</c> verifies but wants a rehash.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public static string HashOf(string password) => $"hashed:{password}";

    public string Hash(string password) => HashOf(password);

    public PasswordCheck Verify(string passwordHash, string password) =>
        passwordHash == HashOf(password) ? PasswordCheck.Succeeded
        : passwordHash == $"old:{password}" ? PasswordCheck.SucceededRehashNeeded
        : PasswordCheck.Failed;
}
