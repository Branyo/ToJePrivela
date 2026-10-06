using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Accounts.Dtos;

/// <summary>
/// Only bounded, not checked against the rules for new logins: a name that breaks them is simply unknown. The password
/// may be empty, so a newcomer who typed only a name learns that it is unknown (and can create it) rather than that a
/// password is missing; an empty password never matches a stored one.
/// </summary>
public sealed class SignInRequest
{
    public const int NameMaxLength = 256;

    [Required(ErrorMessage = "Login name is required.")]
    [StringLength(NameMaxLength, ErrorMessage = "Login name is too long.")]
    public string Name { get; init; } = default!;

    [StringLength(PasswordRules.MaxLength, ErrorMessage = "Password is too long.")]
    public string? Password { get; init; }
}
