using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Questions;

public static class QuestionErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Question.NotFound", $"Question with id {id} was not found.");

    public static Error UnknownCategory(string category) =>
        Error.Validation("Question.UnknownCategory", $"Question category '{category}' does not exist.");

    public static readonly Error GenerationFailed =
        Error.Unavailable("Question.GenerationFailed", "No questions could be generated. Please try again.");
}
