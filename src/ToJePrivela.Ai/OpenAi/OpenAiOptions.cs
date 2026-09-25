using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Ai.OpenAi;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    [Required(ErrorMessage = "OpenAi:Url must be configured.")]
    public string Url { get; set; } = "https://api.openai.com/v1/chat/completions";

    /// <summary>Set through user-secrets or the OpenAi__ApiKey environment variable, never in source.</summary>
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = "gpt-6-luna";

    /// <summary>
    /// How long the model reasons before answering. Reasoning lets it check its facts, which keeps
    /// made-up answers out of the generated questions.
    /// </summary>
    [Required]
    [RegularExpression("^(none|low|medium|high|xhigh|max)$",
        ErrorMessage = "OpenAi:ReasoningEffort must be one of none, low, medium, high, xhigh, max.")]
    public string ReasoningEffort { get; set; } = "medium";

    /// <summary>
    /// 0 = deterministic, 2 = maximum randomness. Reasoning requests reject it, so it is sent only when
    /// ReasoningEffort is "none".
    /// </summary>
    [Range(0, 2)]
    public decimal Temperature { get; set; } = 0.2M;

    /// <summary>
    /// Output budget of one call, which carries at most QuestionGeneration:QuestionsPerRequest questions.
    /// Reasoning tokens count against it too.
    /// </summary>
    [Range(1, 128000)]
    public int MaxTokens { get; set; } = 16000;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>Every generated question is written in this language.</summary>
    [Required(ErrorMessage = "OpenAi:Language must be configured.")]
    public string Language { get; set; } = "Slovak";
}
