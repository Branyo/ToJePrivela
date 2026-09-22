using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Players.Dtos;

public sealed class UpdatePlayerRequest
{
    [Required]
    [StringLength(Player.NameMaxLength, MinimumLength = Player.NameMinLength,
        ErrorMessage = "Player name should have from 2 to 50 characters.")]
    public string Name { get; init; } = default!;
}
