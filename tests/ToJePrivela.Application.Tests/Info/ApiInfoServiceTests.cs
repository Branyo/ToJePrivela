using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Info;

namespace ToJePrivela.Application.Tests.Info;

public class ApiInfoServiceTests
{
    [Fact]
    public void Get_ReturnsTheTitleAndTheHostAssemblyVersionWithoutBuildSuffix()
    {
        var result = new ApiInfoService(typeof(ApiInfoServiceTests).Assembly).Get();

        Assert.True(result.IsSuccess);
        Assert.Equal(ApiInfoService.ApiTitle, result.Value.Title);
        Assert.NotEqual(ApiInfoService.UnknownVersion, result.Value.Version);
        Assert.DoesNotContain('+', result.Value.Version);
    }

    [Theory]
    [InlineData("1.2.3+abc123", "1.2.3")]
    [InlineData("1.2.3-beta.1+abc123.dirty", "1.2.3-beta.1")]
    [InlineData("1.2.3", "1.2.3")]
    public void StripBuildMetadata_DropsTheCommitSuffix(string informational, string expected) =>
        Assert.Equal(expected, ApiInfoService.StripBuildMetadata(informational));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("+abc123")]
    public void StripBuildMetadata_ReportsUnknown_WhenThereIsNoVersion(string? informational) =>
        Assert.Equal(ApiInfoService.UnknownVersion, ApiInfoService.StripBuildMetadata(informational));

    [Fact]
    public void AddApiInfo_RegistersTheServiceAsASingleton()
    {
        var services = new ServiceCollection().AddApiInfo(typeof(ApiInfoServiceTests).Assembly);

        var descriptor = Assert.Single(services);
        Assert.Equal(typeof(IApiInfoService), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.IsType<ApiInfoService>(descriptor.ImplementationInstance);
    }
}
