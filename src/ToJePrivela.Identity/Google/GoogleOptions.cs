namespace ToJePrivela.Identity.Google;

/// <summary>Google sign-in is offered only when <see cref="ClientId"/> is set.</summary>
public sealed class GoogleOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>The OAuth web client id (<c>….apps.googleusercontent.com</c>); public, the browser uses it too.</summary>
    public string? ClientId { get; set; }
}
