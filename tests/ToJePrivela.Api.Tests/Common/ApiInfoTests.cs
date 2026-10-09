using System.Reflection;
using System.Reflection.Emit;
using ToJePrivela.Api.Common;

namespace ToJePrivela.Api.Tests.Common;

public class ApiInfoTests
{
    [Fact]
    public void FromAssembly_ReportsTheStampedVersionWithoutBuildSuffix()
    {
        var info = ApiInfo.FromAssembly(AssemblyWithInformationalVersion("2.5.1+0123abcd"));

        Assert.Equal(ApiInfo.ApiTitle, info.Title);
        Assert.Equal("2.5.1", info.Version);
    }

    [Fact]
    public void FromAssembly_ReportsUnknown_WhenTheBuildStampedNoVersion()
    {
        var info = ApiInfo.FromAssembly(AssemblyWithInformationalVersion(null));

        Assert.Equal(ApiInfo.UnknownVersion, info.Version);
    }

    [Theory]
    [InlineData("1.2.3+abc123", "1.2.3")]
    [InlineData("1.2.3-beta.1+abc123.dirty", "1.2.3-beta.1")]
    [InlineData("1.2.3", "1.2.3")]
    public void StripBuildMetadata_DropsTheCommitSuffix(string informational, string expected) =>
        Assert.Equal(expected, ApiInfo.StripBuildMetadata(informational));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("+abc123")]
    public void StripBuildMetadata_ReportsUnknown_WhenThereIsNoVersion(string? informational) =>
        Assert.Equal(ApiInfo.UnknownVersion, ApiInfo.StripBuildMetadata(informational));

    private static Assembly AssemblyWithInformationalVersion(string? informationalVersion)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"Versioned{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);

        if (informationalVersion is not null)
        {
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!,
                [informationalVersion]));
        }

        return assembly;
    }
}
