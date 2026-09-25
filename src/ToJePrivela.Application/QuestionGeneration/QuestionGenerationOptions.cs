using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.QuestionGeneration;

public sealed class QuestionGenerationOptions
{
    public const string SectionName = "QuestionGeneration";

    /// <summary>Upper bound of questions one request may ask for; request DTOs validate against it.</summary>
    public const int MaxCount = 200;

    /// <summary>Questions asked for in one AI call; keeps each reply well inside the token budget.</summary>
    [Range(1, 50)]
    public int QuestionsPerRequest { get; set; } = 20;

    /// <summary>Extra calls allowed on top of the planned ones, to top up duplicates and dropped items.</summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(1, 10)]
    public int MaxParallelRequests { get; set; } = 3;

    /// <summary>How many known questions each call is told not to repeat.</summary>
    [Range(0, 500)]
    public int MaxExcludedQuestions { get; set; } = 100;
}
