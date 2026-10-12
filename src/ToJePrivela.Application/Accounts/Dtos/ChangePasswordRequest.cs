using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Accounts.Dtos;

/// <summary>
/// The current password proves it is the login's holder (not just someone at a device left signed in). Repeating the
/// new password is the client's job; the API takes it once.
/// </summary>
public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required.")]
    [StringLength(PasswordRules.MaxLength, ErrorMessage = "Current password is too long.")]
    public string CurrentPassword { get; init; } = default!;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength,
        ErrorMessage = "New password should have from {2} to {1} characters.")]
    public string NewPassword { get; init; } = default!;
}
