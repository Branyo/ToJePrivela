using System.Reflection;

namespace ToJePrivela.Api.Common;

/// <summary>
/// Name and version of the running API, served by <c>GET /api/info</c>. Both are host facts, so they are read once at
/// startup from the API assembly rather than going through an Application use case.
/// </summary>
public sealed record ApiInfo(string Title, string Version)
{
    public const string ApiTitle = "ToJePrivela API";
    public const string UnknownVersion = "unknown";

    /// <summary>
    /// Reads the informational version of <paramref name="assembly"/>. The API project only stamps one when the build
    /// passes a version (<c>-p:Version=…</c>), so an unversioned build reports <see cref="UnknownVersion"/> instead of
    /// the SDK's plausible but wrong "1.0.0".
    /// </summary>
    public static ApiInfo FromAssembly(Assembly assembly) =>
        new(ApiTitle, StripBuildMetadata(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));

    /// <summary>The informational version may carry a "+commit" build suffix; clients only need the version itself.</summary>
    public static string StripBuildMetadata(string? informationalVersion)
    {
        var core = informationalVersion?.Split('+')[0].Trim();

        return string.IsNullOrEmpty(core) ? UnknownVersion : core;
    }
}
