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
    public string Model { get; set; } = "gpt-3.5-turbo";

    /// <summary>0 = deterministic, 2 = maximum randomness.</summary>
    [Range(0, 2)]
    public decimal Temperature { get; set; } = 0.8M;

    /// <summary>Output budget of one call, which carries at most QuestionGeneration:QuestionsPerRequest questions.</summary>
    [Range(1, 16000)]
    public int MaxTokens { get; set; } = 4000;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Every generated question is written in this language.</summary>
    [Required(ErrorMessage = "OpenAi:Language must be configured.")]
    public string Language { get; set; } = "Slovak";
}
