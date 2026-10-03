namespace ToJePrivela.Identity.Tokens;

/// <summary>What an access token says about its account; the host reads the same names back.</summary>
public static class AccessTokenClaims
{
    /// <summary>The account id.</summary>
    public const string Subject = "sub";

    public const string Name = "name";

    public const string Role = "role";

    /// <summary>The <see cref="Role"/> of an admin account.</summary>
    public const string AdminRole = "admin";
}
