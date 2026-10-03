using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Identity.Facebook;

/// <summary>Facebook sign-in is offered only when both <see cref="AppId"/> and <see cref="AppSecret"/> are set.</summary>
public sealed class FacebookOptions
{
    public const string SectionName = "Authentication:Facebook";

    /// <summary>Public; the browser's SDK is initialised with it.</summary>
    public string? AppId { get; set; }

    /// <summary>Set through user-secrets or the Authentication__Facebook__AppSecret environment variable, never in source.</summary>
    public string? AppSecret { get; set; }

    [Required(ErrorMessage = "Authentication:Facebook:GraphApiUrl must be configured.")]
    [Url]
    public string GraphApiUrl { get; set; } = "https://graph.facebook.com/v23.0";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppSecret);
}
