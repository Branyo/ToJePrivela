using System.Reflection;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Info.Dtos;

namespace ToJePrivela.Application.Info;

/// <summary>Names and versions the running API; the version is read from the assembly the host passes in.</summary>
public sealed class ApiInfoService : IApiInfoService
{
    public const string ApiTitle = "ToJePrivela API";

    private readonly ApiInfoDto _info;

    public ApiInfoService(Assembly hostAssembly)
    {
        _info = new ApiInfoDto(ApiTitle, ReadVersion(hostAssembly));
    }

    public Result<ApiInfoDto> Get() => Result.Success(_info);

    // The informational version may carry a "+commit" build suffix; clients only need the version itself.
    private static string ReadVersion(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var core = informational?.Split('+')[0];

        return !string.IsNullOrWhiteSpace(core) ? core : assembly.GetName().Version?.ToString(3) ?? "unknown";
    }
}
