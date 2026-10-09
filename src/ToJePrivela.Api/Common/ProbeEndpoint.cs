namespace ToJePrivela.Api.Common;

/// <summary>
/// Marks an endpoint that load balancers and orchestrators poll (<c>/api/health</c>). Middleware asks
/// <see cref="ProbeEndpoint.IsProbe"/> instead of matching paths, so a new probe endpoint only needs
/// <see cref="ProbeEndpoint.AsProbe{TBuilder}"/>.
/// </summary>
public sealed class ProbeEndpointMetadata
{
    public static readonly ProbeEndpointMetadata Instance = new();

    private ProbeEndpointMetadata()
    {
    }
}

public static class ProbeEndpoint
{
    public static TBuilder AsProbe<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(ProbeEndpointMetadata.Instance);

    /// <summary>Whether the request was routed to a probe endpoint; routing runs first, so every middleware can ask.</summary>
    public static bool IsProbe(this HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<ProbeEndpointMetadata>() is not null;
}
