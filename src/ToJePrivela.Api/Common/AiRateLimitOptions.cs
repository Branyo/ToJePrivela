using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Api.Common;

/// <summary>Fixed-window limit shared by every request that pays for AI calls (see <see cref="AiRateLimiter"/>).</summary>
public sealed class AiRateLimitOptions
{
    public const string SectionName = "RateLimiting:AiGeneration";

    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 5;

    [Range(1, 86400)]
    public int WindowSeconds { get; set; } = 60;
}
