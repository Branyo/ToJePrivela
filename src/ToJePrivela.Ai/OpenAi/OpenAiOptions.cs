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

    [Range(1, 16000)]
    public int MaxTokens { get; set; } = 2000;

    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 60;

    public string DefaultCategory { get; set; } = "Arbitrary";

    public string DefaultLanguage { get; set; } = "Slovak";

    [Range(1, 5)]
    public int DefaultDifficulty { get; set; } = 3;
}
