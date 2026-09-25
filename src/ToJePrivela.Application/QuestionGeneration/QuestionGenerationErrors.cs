using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.QuestionGeneration;

public static class QuestionGenerationErrors
{
    public static readonly Error GenerationFailed =
        Error.Unavailable("QuestionGeneration.Failed", "No questions could be generated. Please try again.");
}
