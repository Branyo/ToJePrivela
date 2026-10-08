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
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Version));
        Assert.DoesNotContain('+', result.Value.Version);
    }
}
