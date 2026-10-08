using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ToJePrivela.Application.Info;

public static class InfoServiceCollectionExtensions
{
    /// <summary>Registers the Info slice; the version it reports is the one of <paramref name="hostAssembly"/>, the running API.</summary>
    public static IServiceCollection AddApiInfo(this IServiceCollection services, Assembly hostAssembly) =>
        services.AddSingleton<IApiInfoService>(new ApiInfoService(hostAssembly));
}
