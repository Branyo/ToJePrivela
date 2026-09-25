using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration.Dtos;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class QuestionCategoriesEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public QuestionCategoriesEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ReplyWithFreshQuestions();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsTheSeededCategoriesWithTheirAuthor()
    {
        var categories = await _client.GetFromJsonAsync<List<QuestionCategoryDto>>("/api/question-categories");

        var seeded = categories!.Where(category => category.Id <= 3).ToList();
        Assert.Equal(["Cars", "Sport", "History"], seeded.Select(category => category.Name));
        Assert.All(seeded, category => Assert.Equal("Admin", category.AddedByPlayer?.Name));
    }

    [Fact]
    public async Task PostCategory_WithZeroQuestionsCreatesJustTheCategory()
    {
        var name = NewName();

        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name, addedByPlayerId = 1, questionCount = 0 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<CreatedQuestionCategoryDto>();
        Assert.Equal(name, created!.Name);
        Assert.Equal(new QuestionGenerationSummaryDto(0, 0, 0), created.QuestionGeneration);
        Assert.Empty(await GetQuestionsAsync(created.Id));
    }

    [Fact]
    public async Task PostCategory_StoresTheGeneratedQuestions()
    {
        var created = await CreateCategoryAsync(questionCount: 3);

        Assert.Equal(new QuestionGenerationSummaryDto(3, 3, 0), created.QuestionGeneration);

        var questions = await GetQuestionsAsync(created.Id);
        Assert.Equal(3, questions.Count);
        Assert.All(questions, question =>
        {
            Assert.Equal("Ai", question.Source);
            Assert.Equal(created.Name, question.CategoryName);
            Assert.InRange(question.BadPoints, 1, 5);
        });
    }

    [Fact]
    public async Task PostCategory_CreatesNothingWhenGenerationFails()
    {
        _factory.ReplyWithNothing();
        var name = NewName();

        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name, questionCount = 5 });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var categories = await _client.GetFromJsonAsync<List<QuestionCategoryDto>>("/api/question-categories");
        Assert.DoesNotContain(categories!, category => category.Name == name);
    }

    [Fact]
    public async Task PostCategory_RequiresTheQuestionCount()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name = NewName() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(201)]
    public async Task PostCategory_RejectsAQuestionCountOutOfRange(int questionCount)
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name = NewName(), questionCount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCategory_RejectsADuplicateNameIgnoringCase()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name = "sport", questionCount = 0 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostCategory_RejectsAnUnknownAuthor()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { name = NewName(), addedByPlayerId = 9999, questionCount = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCategory_IsNotSupportedBecauseCategoriesAreImmutable()
    {
        var created = await CreateCategoryAsync();

        var response = await _client.PutAsJsonAsync($"/api/question-categories/{created.Id}", new { name = NewName() });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_RemovesTheCategoryAndAllOfItsQuestions()
    {
        var created = await CreateCategoryAsync(questionCount: 2);
        var manual = await CreateManualQuestionAsync(created.Id);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await _client.DeleteAsync($"/api/question-categories/{created.Id}")).StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/question-categories/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/questions/{manual.Id}")).StatusCode);
        Assert.Empty(await GetQuestionsAsync(created.Id));
    }

    [Fact]
    public async Task PostAiQuestions_AddsQuestionsToTheCategory()
    {
        var created = await CreateCategoryAsync(questionCount: 2);

        var response = await _client.PostAsJsonAsync($"/api/question-categories/{created.Id}/ai-questions", new { count = 4 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var generated = await response.Content.ReadFromJsonAsync<GeneratedAiQuestionsDto>();
        Assert.Equal(new QuestionGenerationSummaryDto(4, 4, 0), generated!.Summary);
        Assert.Equal(4, generated.Questions.Count);
        Assert.All(generated.Questions, question => Assert.True(question.Id > 0));
        Assert.Equal(6, (await GetQuestionsAsync(created.Id)).Count);
    }

    [Fact]
    public async Task PostAiQuestions_ReportsUnavailableWhenNothingComesBack()
    {
        var created = await CreateCategoryAsync();
        _factory.ReplyWithNothing();

        var response = await _client.PostAsJsonAsync($"/api/question-categories/{created.Id}/ai-questions", new { count = 4 });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task PostAiQuestions_RejectsACountOutOfRange(int count)
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories/1/ai-questions", new { count });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAiQuestions_ReturnsNotFoundForUnknownCategory()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories/9999/ai-questions", new { count = 4 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAiQuestions_KeepsTheManualQuestions()
    {
        var created = await CreateCategoryAsync(questionCount: 3);
        var manual = await CreateManualQuestionAsync(created.Id);

        var response = await _client.DeleteAsync($"/api/question-categories/{created.Id}/ai-questions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, (await response.Content.ReadFromJsonAsync<DeletedAiQuestionsDto>())!.Deleted);
        Assert.Equal(manual.Id, Assert.Single(await GetQuestionsAsync(created.Id)).Id);
    }

    [Fact]
    public async Task DeleteAiQuestions_ReturnsNotFoundForUnknownCategory()
    {
        var response = await _client.DeleteAsync("/api/question-categories/9999/ai-questions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string NewName() => $"Cat{Guid.NewGuid():N}"[..12];

    private async Task<CreatedQuestionCategoryDto> CreateCategoryAsync(int questionCount = 0)
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { name = NewName(), questionCount });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CreatedQuestionCategoryDto>())!;
    }

    private async Task<QuestionDto> CreateManualQuestionAsync(int categoryId)
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new
        {
            text = "How many players are on a football pitch?",
            answer = "22",
            categoryId,
            badPoints = 2
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<QuestionDto>())!;
    }

    private async Task<List<QuestionDto>> GetQuestionsAsync(int categoryId) =>
        (await _client.GetFromJsonAsync<List<QuestionDto>>($"/api/questions?categoryId={categoryId}"))!;
}
