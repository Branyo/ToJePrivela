using System.Net;
using System.Net.Http.Json;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Everything needs a signed-in account; changing the shared questions and categories needs an admin.</summary>
public class AuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _anonymous;
    private readonly HttpClient _member;
    private readonly HttpClient _admin;

    public AuthorizationTests(ApiFactory factory)
    {
        _anonymous = factory.CreateAnonymousClient();
        _member = factory.CreateClientAs(factory.Member);
        _admin = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/players")]
    [InlineData("/api/games")]
    [InlineData("/api/questions")]
    [InlineData("/api/question-categories")]
    public async Task ReadingWithoutSigningIn_Answers401(string url)
    {
        var response = await _anonymous.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.Unauthenticated", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task Rules_AreReadableWithoutSigningIn()
    {
        var response = await _anonymous.GetAsync("/api/rules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/players")]
    [InlineData("/api/games")]
    [InlineData("/api/questions")]
    [InlineData("/api/question-categories")]
    public async Task ReadingAsAMember_Works(string url)
    {
        var response = await _member.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreatingACategoryAsAMember_Answers403()
    {
        var response = await _member.PostAsJsonAsync(
            "/api/question-categories",
            new { nameSk = $"C{Guid.NewGuid():N}"[..12], questionCount = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Auth.Forbidden", await ProblemResponse.CodeOf(response));
    }

    [Theory]
    [InlineData("POST", "/api/question-categories/1/ai-questions")]
    [InlineData("DELETE", "/api/question-categories/1/ai-questions")]
    [InlineData("DELETE", "/api/question-categories/1")]
    [InlineData("POST", "/api/questions")]
    [InlineData("PUT", "/api/questions/1")]
    [InlineData("DELETE", "/api/questions/1")]
    public async Task ChangingSharedQuestionsAsAMember_Answers403(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { count = 1, textSk = "Q?", textEn = "Q?", answer = "1", categoryId = 1 })
        };

        var response = await _member.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatingACategoryAsAnAdmin_Works()
    {
        var response = await _admin.PostAsJsonAsync(
            "/api/question-categories",
            new { nameSk = $"C{Guid.NewGuid():N}"[..12], questionCount = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RecordingAViewAsAMember_Works()
    {
        var category = await (await _admin.PostAsJsonAsync(
                "/api/question-categories",
                new { nameSk = $"C{Guid.NewGuid():N}"[..12], questionCount = 1 }))
            .Content.ReadFromJsonAsync<IdOnly>();
        var question = await _member.GetFromJsonAsync<IdOnly>($"/api/questions/random?categoryIds={category!.Id}");

        var response = await _member.PostAsync($"/api/questions/{question!.Id}/views", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record IdOnly(int Id);
}
