using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ToJePrivela.Api.Controllers;

/// <summary>Liveness and identity of the running API, for probes and clients; readable before signing in.</summary>
[ApiController]
[AllowAnonymous]
[Route("api")]
[Produces("application/json")]
public sealed class InfoController : ControllerBase
{
    private const string ApiTitle = "ToJePrivela API";

    private static readonly string ApiVersion = ReadVersion();

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth() => new HealthResponse("Healthy");

    [HttpGet("version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<VersionResponse> GetVersion() => new VersionResponse(ApiVersion);

    [HttpGet("title")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<TitleResponse> GetTitle() => new TitleResponse(ApiTitle);

    // The informational version may carry a "+commit" build suffix; clients only need the version itself.
    private static string ReadVersion()
    {
        var assembly = typeof(InfoController).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return informational?.Split('+')[0] ?? assembly.GetName().Version?.ToString(3) ?? "unknown";
    }
}

public sealed record HealthResponse(string Status);

public sealed record VersionResponse(string Version);

public sealed record TitleResponse(string Title);
