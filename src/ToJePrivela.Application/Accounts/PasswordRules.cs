namespace ToJePrivela.Application.Accounts;

/// <summary>
/// What a new password must be. Only its length is checked (as NIST SP 800-63B advises); the upper bound keeps the
/// deliberately slow hashing from being fed megabytes.
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 6;
    public const int MaxLength = 128;
}
