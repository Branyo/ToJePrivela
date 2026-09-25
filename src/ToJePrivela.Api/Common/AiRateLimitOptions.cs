using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Api.Common;

/// <summary>Fixed-window limit shared by every endpoint that pays for AI generation.</summary>
public sealed class AiRateLimitOptions
{
    public const string SectionName = "RateLimiting:AiGeneration";
    public const string PolicyName = "ai-generation";

    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 5;

    [Range(1, 86400)]
    public int WindowSeconds { get; set; } = 60;
}
