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
    public async Task GetCategories_ReturnsTheStoredCategories()
    {
        var categories = await _client.GetFromJsonAsync<List<QuestionCategoryDto>>("/api/question-categories");

        // The factory's test categories; other tests in this class add more.
        var testCategories = categories!.Where(category => category.Id <= 3).ToList();
        Assert.Equal(["Autá", "Šport", "História"], testCategories.Select(category => category.Name));
        Assert.Equal(["Cars", "Sport", "History"], testCategories.Select(category => category.NameEn));
    }

    [Theory]
    [InlineData("en", "Sport")]
    [InlineData("en-GB,en;q=0.9", "Sport")]
    [InlineData("de, en;q=0.5, sk;q=0.4", "Sport")]
    [InlineData("sk", "Šport")]
    [InlineData("de", "Šport")]
    public async Task GetCategory_NamesItInTheLanguageTheRequestAsksFor(string acceptLanguage, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/question-categories/2");
        request.Headers.Add("Accept-Language", acceptLanguage);

        var response = await _client.SendAsync(request);

        var category = await response.Content.ReadFromJsonAsync<QuestionCategoryDto>();
        Assert.Equal(name, category!.Name);
    }

    [Fact]
    public async Task PostCategory_TranslatesTheMissingName()
    {
        var name = NewName();

        var response = await _client.PostAsJsonAsync("/api/question-categories", new { nameEn = name, questionCount = 0 });

        var created = await response.Content.ReadFromJsonAsync<CreatedQuestionCategoryDto>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(($"{name} (Sk)", name), (created!.NameSk, created.NameEn));
    }

    [Fact]
    public async Task PostCategory_RequiresAName()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { questionCount = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCategory_WithZeroQuestionsCreatesJustTheCategory()
    {
        var name = NewName();

        var response = await _client.PostAsJsonAsync("/api/question-categories", new { nameSk = name, questionCount = 0 });

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
    public async Task PostCategory_StoresTheGeneratedQuestionsInBothLanguages()
    {
        var created = await CreateCategoryAsync(questionCount: 1);

        var question = Assert.Single(await GetQuestionsAsync(created.Id));
        Assert.StartsWith("Vygenerovaná testová otázka", question.TextSk);
        Assert.StartsWith("Generated test question", question.TextEn);
        Assert.Equal(question.TextSk, question.Text);
    }

    [Fact]
    public async Task PostCategory_CreatesNothingWhenGenerationFails()
    {
        _factory.ReplyWithNothing();
        var name = NewName();

        var response = await _client.PostAsJsonAsync("/api/question-categories", new { nameSk = name, questionCount = 5 });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var categories = await _client.GetFromJsonAsync<List<QuestionCategoryDto>>("/api/question-categories");
        Assert.DoesNotContain(categories!, category => category.Name == name);
    }

    [Fact]
    public async Task PostCategory_RequiresTheQuestionCount()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { nameSk = NewName() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(201)]
    public async Task PostCategory_RejectsAQuestionCountOutOfRange(int questionCount)
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { nameSk = NewName(), questionCount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCategory_RejectsADuplicateNameIgnoringCase()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { nameSk = "Futbal", nameEn = "sport", questionCount = 0 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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
        var response = await _client.PostAsJsonAsync("/api/question-categories", new { nameSk = NewName(), questionCount });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CreatedQuestionCategoryDto>())!;
    }

    private async Task<QuestionDto> CreateManualQuestionAsync(int categoryId)
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new
        {
            textSk = "How many players are on a football pitch?",
            textEn = "How many players are on a football pitch?",
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
