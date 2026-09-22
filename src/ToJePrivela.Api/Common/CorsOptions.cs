namespace ToJePrivela.Api.Common;

/// <summary>Origins of the frontend that will consume this API.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public const string PolicyName = "FrontendPolicy";

    public string[] AllowedOrigins { get; set; } = [];
}
