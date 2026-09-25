using System.Net;
using System.Net.Http.Json;

namespace ToJePrivela.Api.Tests.Integration;

public sealed class SingleAiPermitApiFactory : ApiFactory
{
    protected override int AiGenerationPermitLimit => 1;
}

public class AiRateLimitTests : IClassFixture<SingleAiPermitApiFactory>
{
    private readonly HttpClient _client;

    public AiRateLimitTests(SingleAiPermitApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AiEndpointsShareOneLimitAndRejectTheExcessWith429()
    {
        var first = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { name = $"Cat{Guid.NewGuid():N}"[..12], questionCount = 1 });
        var second = await _client.PostAsJsonAsync("/api/question-categories/1/ai-questions", new { count = 1 });
        var unlimited = await _client.GetAsync("/api/question-categories");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unlimited.StatusCode);
    }
}
