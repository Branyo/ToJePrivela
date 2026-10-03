using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts;

/// <summary>
/// The logins that are admins, stored into the database on startup (see <see cref="IAdminAccountProvisioner"/>).
/// Kept in configuration (appsettings, user-secrets or <c>Authentication__Admins__0__Email</c>) so no address is
/// committed to source.
/// </summary>
public sealed class AdminAccountsOptions
{
    public const string SectionName = "Authentication";

    public List<AdminLogin> Admins { get; set; } = [];
}

public sealed class AdminLogin
{
    [Required(ErrorMessage = "Authentication:Admins needs a Provider for every admin.")]
    [EnumDataType(typeof(IdentityProvider), ErrorMessage = "Authentication:Admins Provider should be Google or Facebook.")]
    public IdentityProvider? Provider { get; set; }

    [Required(ErrorMessage = "Authentication:Admins needs an Email for every admin.")]
    [EmailAddress(ErrorMessage = "Authentication:Admins Email must be an email address.")]
    [StringLength(Account.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;
}
