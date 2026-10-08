using System.Reflection;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Info.Dtos;

namespace ToJePrivela.Application.Info;

/// <summary>Names and versions the running API; the version is read from the assembly the host passes in.</summary>
public sealed class ApiInfoService : IApiInfoService
{
    public const string ApiTitle = "ToJePrivela API";
    public const string UnknownVersion = "unknown";

    private readonly ApiInfoDto _info;

    public ApiInfoService(Assembly hostAssembly)
    {
        _info = new ApiInfoDto(ApiTitle, ReadVersion(hostAssembly));
    }

    public Result<ApiInfoDto> Get() => Result.Success(_info);

    // No fallback to the assembly version: an unversioned build would report a plausible but wrong "1.0.0".
    private static string ReadVersion(Assembly assembly) =>
        StripBuildMetadata(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>The informational version may carry a "+commit" build suffix; clients only need the version itself.</summary>
    public static string StripBuildMetadata(string? informationalVersion)
    {
        var core = informationalVersion?.Split('+')[0].Trim();

        return string.IsNullOrEmpty(core) ? UnknownVersion : core;
    }
}
