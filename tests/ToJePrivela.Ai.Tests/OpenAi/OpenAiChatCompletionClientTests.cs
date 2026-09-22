using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ToJePrivela.Ai.OpenAi;

namespace ToJePrivela.Ai.Tests.OpenAi;

public class OpenAiChatCompletionClientTests
{
    private const string SuccessBody = """
        {"choices": [{"message": {"role": "assistant", "content": "[{\"question\": \"q\", \"answer\": 1}]"}}]}
        """;

    private static readonly OpenAiOptions Options = new()
    {
        Url = "https://api.example.test/v1/chat/completions",
        ApiKey = "test-key",
        Model = "gpt-3.5-turbo",
        MaxTokens = 1234,
        Temperature = 0.5M
    };

    [Fact]
    public async Task CompleteAsync_ReturnsTheAssistantReply()
    {
        var handler = StubHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessBody);

        var reply = await CreateSut(handler).CompleteAsync("prompt");

        Assert.Equal("""[{"question": "q", "answer": 1}]""", reply);
    }

    [Fact]
    public async Task CompleteAsync_SendsTheConfiguredModelAndPrompt()
    {
        var handler = StubHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessBody);

        await CreateSut(handler).CompleteAsync("generate questions");

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("gpt-3.5-turbo", handler.LastRequestBody);
        Assert.Contains("generate questions", handler.LastRequestBody);
        Assert.Contains("\"max_tokens\":1234", handler.LastRequestBody);
        Assert.Contains("\"temperature\":0.5", handler.LastRequestBody);
    }

    [Fact]
    public async Task CompleteAsync_SendsTheApiKeyAsBearerToken()
    {
        var handler = StubHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessBody);

        await CreateSut(handler).CompleteAsync("prompt");

        Assert.Equal("Bearer", handler.LastRequest?.Headers.Authorization?.Scheme);
        Assert.Equal("test-key", handler.LastRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task CompleteAsync_PostsToTheConfiguredUrl()
    {
        var handler = StubHttpMessageHandler.WithJson(HttpStatusCode.OK, SuccessBody);

        await CreateSut(handler).CompleteAsync("prompt");

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal(new Uri(Options.Url), handler.LastRequest?.RequestUri);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CompleteAsync_ReturnsNullOnErrorStatus(HttpStatusCode statusCode)
    {
        var handler = StubHttpMessageHandler.WithJson(statusCode, "{}");

        Assert.Null(await CreateSut(handler).CompleteAsync("prompt"));
    }

    [Fact]
    public async Task CompleteAsync_ReturnsNullWhenTheBodyIsNotValidJson()
    {
        var handler = StubHttpMessageHandler.WithJson(HttpStatusCode.OK, "not json");

        Assert.Null(await CreateSut(handler).CompleteAsync("prompt"));
    }

    [Fact]
    public async Task CompleteAsync_ReturnsNullWhenThereAreNoChoices()
    {
        var handler = StubHttpMessageHandler.WithJson(HttpStatusCode.OK, """{"choices": []}""");

        Assert.Null(await CreateSut(handler).CompleteAsync("prompt"));
    }

    [Fact]
    public async Task CompleteAsync_ReturnsNullWhenTheTransportFails()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("network down"));

        Assert.Null(await CreateSut(handler).CompleteAsync("prompt"));
    }

    private static OpenAiChatCompletionClient CreateSut(StubHttpMessageHandler handler) => new(
        new HttpClient(handler),
        Microsoft.Extensions.Options.Options.Create(Options),
        NullLogger<OpenAiChatCompletionClient>.Instance);
}
