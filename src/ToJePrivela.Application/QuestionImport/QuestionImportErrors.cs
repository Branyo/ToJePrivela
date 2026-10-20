using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.QuestionImport;

public static class QuestionImportErrors
{
    public static readonly Error InvalidJson =
        Error.Validation(
            "QuestionImport.InvalidJson",
            "The text is not a JSON array of questions such as " +
            "[{\"questionSk\": \"…\", \"questionEn\": \"…\", \"answer\": 42}].");

    public static readonly Error Empty =
        Error.Validation("QuestionImport.Empty", "The JSON array holds no questions.");

    public static Error TooMany(int count) =>
        Error.Validation(
            "QuestionImport.TooMany",
            $"The JSON array holds {count} questions; one import takes at most {QuestionImporter.MaxQuestions}.");
}
