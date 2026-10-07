namespace ToJePrivela.Identity.Tokens;

/// <summary>What an access token says about its account, and the claims the host adds; both sides use these names.</summary>
public static class AccessTokenClaims
{
    /// <summary>The account id.</summary>
    public const string Subject = "sub";

    public const string Name = "name";

    /// <summary>
    /// The account's security stamp when the token was issued; the host refuses the token once the stored one differs.
    /// </summary>
    public const string SecurityStamp = "stamp";

    /// <summary>Not in the token: the host adds it for an admin once it has read the stored account.</summary>
    public const string Role = "role";

    /// <summary>The <see cref="Role"/> of an admin account.</summary>
    public const string AdminRole = "admin";
}
