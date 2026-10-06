using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.Common.Validation;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts.Dtos;

/// <summary>Repeating the password is the client's job; the API takes it once.</summary>
public sealed class CreateAccountRequest
{
    [Required(ErrorMessage = "Login name is required.")]
    [TrimmedLength(Account.NameMinLength, Account.NameMaxLength,
        ErrorMessage = "Login name should have from {1} to {2} characters.")]
    public string Name { get; init; } = default!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength,
        ErrorMessage = "Password should have from {2} to {1} characters.")]
    public string Password { get; init; } = default!;
}
