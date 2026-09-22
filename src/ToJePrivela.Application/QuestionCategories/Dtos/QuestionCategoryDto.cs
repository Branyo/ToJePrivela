using ToJePrivela.Application.Players.Dtos;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

public sealed record QuestionCategoryDto(int Id, string Name, PlayerDto? AddedByPlayer);
