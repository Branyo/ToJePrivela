namespace ToJePrivela.Api.Common;

/// <summary>
/// Every endpoint needs a signed-in account unless it says <c>[AllowAnonymous]</c>; the ones that change the shared
/// questions and categories (AI generation included) and the ones under <c>/api/admin</c> (every login's data) also
/// need <see cref="Admin"/>.
/// </summary>
public static class AuthorizationPolicies
{
    public const string Admin = "admin";
}
