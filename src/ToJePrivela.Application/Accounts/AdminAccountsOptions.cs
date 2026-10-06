using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.Common.Validation;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts;

/// <summary>
/// The admin logins, stored into the database on startup (see <see cref="IAdminAccountProvisioner"/>). Passwords
/// belong in user-secrets or environment variables (<c>Authentication__Admins__0__Password</c>), never in source.
/// </summary>
public sealed class AdminAccountsOptions
{
    public const string SectionName = "Authentication";

    public List<AdminLogin> Admins { get; set; } = [];
}

public sealed class AdminLogin
{
    [Required(ErrorMessage = "Authentication:Admins needs a Name for every admin.")]
    [TrimmedLength(Account.NameMinLength, Account.NameMaxLength,
        ErrorMessage = "Authentication:Admins Name should have from {1} to {2} characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Authentication:Admins needs a Password for every admin.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength,
        ErrorMessage = "Authentication:Admins Password should have from {2} to {1} characters.")]
    public string Password { get; set; } = string.Empty;
}
