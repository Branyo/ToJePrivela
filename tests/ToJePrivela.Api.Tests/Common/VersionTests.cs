using System.Text.Json;
using System.Text.RegularExpressions;
using ToJePrivela.Api.Common;

namespace ToJePrivela.Api.Tests.Common;

/// <summary>The game has one version: VERSION, stamped on the API and repeated by the frontend's package files.</summary>
public partial class VersionTests
{
    [Fact]
    public void TheVersion_IsASemanticVersion() =>
        Assert.Matches(SemanticVersion(), GameVersion.Read());

    [Fact]
    public void TheApi_CarriesTheVersion() =>
        Assert.Equal(GameVersion.Read(), ApiInfo.FromAssembly(typeof(Program).Assembly).Version);

    [Theory]
    [InlineData("frontend/package.json")]
    [InlineData("frontend/package-lock.json")]
    public void TheFrontend_CarriesTheVersion(string file)
    {
        using var package = JsonDocument.Parse(File.ReadAllText(GameVersion.PathOf(file)));

        Assert.Equal(GameVersion.Read(), package.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public void TheChangelog_DescribesTheVersion() =>
        Assert.Contains($"## [{GameVersion.Read()}]", File.ReadAllText(GameVersion.PathOf("CHANGELOG.md")));

    /// <summary>https://semver.org: MAJOR.MINOR.PATCH, an optional pre-release, no build metadata.</summary>
    [GeneratedRegex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$")]
    private static partial Regex SemanticVersion();
}
