using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Questions;

public static class QuestionErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Question.NotFound", $"Question with id {id} was not found.");

    public static Error UnknownCategory(int categoryId) =>
        Error.Validation("Question.UnknownCategory", $"Question category with id {categoryId} does not exist.");

    public static Error InvalidCategoryIds(IEnumerable<int> categoryIds) =>
        Error.Validation("Question.InvalidCategoryIds", $"Category ids should be positive: {string.Join(", ", categoryIds)}.");

    public static Error UnknownCategories(IEnumerable<int> categoryIds) =>
        Error.Validation("Question.UnknownCategories", $"Question categories with ids {string.Join(", ", categoryIds)} do not exist.");

    public static Error NoneAvailable() =>
        Error.NotFound("Question.NoneAvailable", "The selected categories contain no questions.");

    public static Error ViewConflict(int id) =>
        Error.Conflict("Question.ViewConflict", $"The view of question {id} could not be recorded because of concurrent changes. Try again.");
}
