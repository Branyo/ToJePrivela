using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.QuestionGeneration;

public static class QuestionGenerationErrors
{
    public static readonly Error GenerationFailed =
        Error.Unavailable("QuestionGeneration.Failed", "No questions could be generated. Please try again.");

    public static readonly Error ProviderUnavailable =
        Error.Unavailable(
            "QuestionGeneration.ProviderUnavailable",
            "The AI question service is not available right now (not configured, unreachable or out of quota).");

    /// <summary>Why a <see cref="QuestionGenerationResult.Failed"/> generation produced nothing.</summary>
    public static Error For(QuestionGenerationResult result) =>
        result.ProviderUnavailable ? ProviderUnavailable : GenerationFailed;
}
