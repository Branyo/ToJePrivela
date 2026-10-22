using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ToJePrivela.Api.Common;

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
            new { nameSk = $"Cat{Guid.NewGuid():N}"[..12], questionCount = 1 });
        var second = await _client.PostAsJsonAsync("/api/question-categories/1/ai-questions", new { count = 1 });
        var unlimited = await _client.GetAsync("/api/question-categories");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(RateLimitRejection.Code, await ProblemResponse.CodeOf(second));
        Assert.Equal(HttpStatusCode.OK, unlimited.StatusCode);
    }
}

public class AiRateLimitSkipTests : IClassFixture<SingleAiPermitApiFactory>
{
    private readonly HttpClient _client;

    public AiRateLimitSkipTests(SingleAiPermitApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatingACategoryWithoutAiCallsDoesNotCountTowardsTheLimit()
    {
        const string json = """[{"questionSk": "Koľko metrov meria Eiffelova veža?", "questionEn": "How many meters tall is the Eiffel Tower?", "answer": 330}]""";

        var imported = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { nameSk = NewName(), nameEn = NewName(), questionCount = 0, questionsJson = json });
        var empty = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { nameSk = NewName(), nameEn = NewName(), questionCount = 0 });
        var generated = await _client.PostAsJsonAsync("/api/question-categories/1/ai-questions", new { count = 1 });
        var translated = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { nameSk = NewName(), questionCount = 0 });

        Assert.Equal(HttpStatusCode.Created, imported.StatusCode);
        Assert.Equal(HttpStatusCode.Created, empty.StatusCode);
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, translated.StatusCode);
        Assert.True(translated.Headers.Contains("Retry-After"));
    }

    private static string NewName() => $"Cat{Guid.NewGuid():N}"[..12];
}

public sealed class NoAiPermitApiFactory : ApiFactory
{
    protected override int AiGenerationPermitLimit => 0;
}

public class RateLimitOptionsTests
{
    [Fact]
    public void ALimitOutOfRangeStopsTheStartup()
    {
        using var factory = new NoAiPermitApiFactory();

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Equal(nameof(AiRateLimitOptions), exception.OptionsType.Name);
    }
}
