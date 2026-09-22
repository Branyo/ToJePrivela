using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.QuestionCategories.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class QuestionCategoriesEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public QuestionCategoriesEndpointsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsTheSeededCategoriesWithTheirAuthor()
    {
        var categories = await _client.GetFromJsonAsync<List<QuestionCategoryDto>>("/api/question-categories");

        Assert.Contains(categories!, category => category.Name == "Sport");
        Assert.All(categories!, category => Assert.Equal("Admin", category.AddedByPlayer?.Name));
    }

    [Fact]
    public async Task PostCategory_CreatesTheCategory()
    {
        var name = $"Cat{Guid.NewGuid():N}"[..12];

        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name, addedByPlayerId = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<QuestionCategoryDto>();
        Assert.Equal(name, created!.Name);
    }

    [Fact]
    public async Task PostCategory_RejectsADuplicateNameIgnoringCase()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name = "sport" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostCategory_RejectsAnUnknownAuthor()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { name = $"Cat{Guid.NewGuid():N}"[..12], addedByPlayerId = 9999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCategory_RenamesTheCategory()
    {
        var created = await CreateCategoryAsync();
        var newName = $"Ren{Guid.NewGuid():N}"[..12];

        var response = await _client.PutAsJsonAsync($"/api/question-categories/{created.Id}", new { name = newName });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await _client.GetFromJsonAsync<QuestionCategoryDto>($"/api/question-categories/{created.Id}");
        Assert.Equal(newName, updated!.Name);
    }

    [Fact]
    public async Task PutCategory_ReturnsNotFoundForUnknownCategory()
    {
        var response = await _client.PutAsJsonAsync("/api/question-categories/9999", new { name = "Whatever" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_RemovesTheCategory()
    {
        var created = await CreateCategoryAsync();

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await _client.DeleteAsync($"/api/question-categories/{created.Id}")).StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/question-categories/{created.Id}")).StatusCode);
    }

    private async Task<QuestionCategoryDto> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { name = $"Cat{Guid.NewGuid():N}"[..12] });

        return (await response.Content.ReadFromJsonAsync<QuestionCategoryDto>())!;
    }
}
