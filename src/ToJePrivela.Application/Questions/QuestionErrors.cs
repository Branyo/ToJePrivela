using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Questions;

public static class QuestionErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Question.NotFound", $"Question with id {id} was not found.");

    public static Error UnknownCategory(int categoryId) =>
        Error.Validation("Question.UnknownCategory", $"Question category with id {categoryId} does not exist.");
}
