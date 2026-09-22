using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.QuestionCategories;

public static class QuestionCategoryErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("QuestionCategory.NotFound", $"Question category with id {id} was not found.");

    public static Error NameTaken(string name) =>
        Error.Conflict("QuestionCategory.NameTaken", $"Question category with name '{name}' already exists.");

    public static Error UnknownPlayer(int playerId) =>
        Error.Validation("QuestionCategory.UnknownPlayer", $"Player with id {playerId} was not found.");
}
