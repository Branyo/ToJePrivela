using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.Common.Validation;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Players.Dtos;

public sealed class CreatePlayerRequest
{
    [Required]
    [TrimmedLength(Player.NameMinLength, Player.NameMaxLength,
        ErrorMessage = "Player name should have from {1} to {2} characters.")]
    public string Name { get; init; } = default!;
}
