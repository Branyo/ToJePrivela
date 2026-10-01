using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Whoever rejects a request, the answer has the same shape and a code the frontend can translate.</summary>
public class ErrorResponseTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ErrorResponseTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AnnotationsCheckedByAspNet_AnswerWithRequestInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/players", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Request.Invalid", problem.GetProperty("code").GetString());
        Assert.Contains("Name", problem.GetProperty("detail").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task AnUnreadableBody_AnswersWithRequestInvalid()
    {
        var response = await _client.PostAsync(
            "/api/players",
            new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Request.Invalid", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task ANonNumericAnswer_IsRejectedBeforeTheDomain()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/questions",
            new { text = "How many legs has a spider?", answer = "eight", categoryId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Request.Invalid", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task TextThatIsTooShortOnceTrimmed_IsRejectedBeforeTheDomain()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/questions",
            new { text = "     ab        ", answer = "8", categoryId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Request.Invalid", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task TheSamePlayerTwice_IsRejectedBeforeTheDomain()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 1 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Request.Invalid", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task AUseCaseFailure_KeepsItsOwnCode()
    {
        var response = await _client.GetAsync("/api/games/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Game.NotFound", await ProblemResponse.CodeOf(response));
    }
}
