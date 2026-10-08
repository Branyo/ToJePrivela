using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.QuestionCategories;

public static class QuestionCategoryErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("QuestionCategory.NotFound", $"Question category with id {id} was not found.");

    public static Error NameTaken(string name) =>
        Error.Conflict("QuestionCategory.NameTaken", $"Question category with name '{name}' already exists.");

    /// <summary>The AI's translation of the name the client sent is already another category's name.</summary>
    public static Error TranslationNameTaken(string name, string translation) =>
        Error.Conflict(
            "QuestionCategory.TranslationNameTaken",
            $"The translation of '{name}', '{translation}', is already the name of another question category. " +
            "Enter the category name in both languages instead.");

    public static readonly Error TranslationUnavailable =
        Error.Unavailable(
            "QuestionCategory.TranslationUnavailable",
            "The AI translation service is not available right now (not configured, unreachable or out of quota). " +
            "Enter the category name in both languages instead.");

    public static readonly Error TranslationFailed =
        Error.Unavailable(
            "QuestionCategory.TranslationFailed",
            "The category name could not be translated. Try again, or enter the name in both languages.");
}
