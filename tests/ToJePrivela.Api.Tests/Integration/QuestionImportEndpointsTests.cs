using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionImport.Dtos;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class QuestionImportEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Questions =
        """
        ```json
        [{"questionSk": "V ktorom roku bol verejne spustený ChatGPT?", "questionEn": "Which year was ChatGPT publicly released?", "answer": 2022},
         {"questionSk": "Koľko metrov meria Eiffelova veža?", "questionEn": "How many meters tall is the Eiffel Tower?", "answer": "330"},
         {"questionSk": "v ktorom roku bol verejne spustený ChatGPT", "questionEn": "When was ChatGPT released?", "answer": 2022},
         {"questionSk": "Koľko?", "questionEn": "How many?", "answer": 1}]
        ```
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public QuestionImportEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostImportedQuestions_StoresTheUsableOnesAsAiQuestions()
    {
        var category = await CreateCategoryAsync();

        var response = await _client.PostAsJsonAsync($"/api/question-categories/{category.Id}/imported-questions", new { json = Questions });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var imported = (await response.Content.ReadFromJsonAsync<ImportedQuestionsDto>(JsonOptions))!;
        Assert.Equal((4, 2), (imported.Summary.Received, imported.Summary.Imported));
        Assert.Equal(
            [new RejectedQuestionDto(3, QuestionRejection.Repeated), new RejectedQuestionDto(4, QuestionRejection.InvalidText)],
            imported.Summary.Rejected);

        var stored = await _client.GetFromJsonAsync<List<QuestionDto>>($"/api/questions?categoryId={category.Id}");
        Assert.Equal(["2022", "330"], stored!.Select(question => question.Answer).Order());
        Assert.All(stored, question => Assert.Equal("Ai", question.Source));
    }

    [Fact]
    public async Task PostImportedQuestions_SendsTheReasonsAsNames()
    {
        var category = await CreateCategoryAsync();

        var response = await _client.PostAsJsonAsync($"/api/question-categories/{category.Id}/imported-questions", new { json = Questions });

        Assert.Contains("\"reason\":\"Repeated\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PostImportedQuestions_RejectsTextThatIsNotAJsonArray()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories/1/imported-questions", new { json = "{ \"answer\": 1 }" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("QuestionImport.InvalidJson", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task PostImportedQuestions_ReturnsNotFoundForUnknownCategory()
    {
        var response = await _client.PostAsJsonAsync("/api/question-categories/9999/imported-questions", new { json = Questions });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostCategory_CanStartWithImportedQuestions()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { nameSk = $"Cat{Guid.NewGuid():N}"[..12], nameEn = $"Cat{Guid.NewGuid():N}"[..12], questionCount = 0, questionsJson = Questions });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CreatedQuestionCategoryDto>(JsonOptions))!;
        Assert.Equal((2, 2), (created.QuestionCount, created.AiQuestionCount));
        Assert.Equal((4, 2), (created.QuestionImport!.Received, created.QuestionImport.Imported));

        var stored = await _client.GetFromJsonAsync<List<QuestionDto>>($"/api/questions?categoryId={created.Id}");
        Assert.Equal(2, stored!.Count);
    }

    [Fact]
    public async Task PostCategory_WithUnusableJsonCreatesNothing()
    {
        var name = $"Cat{Guid.NewGuid():N}"[..12];

        var response = await _client.PostAsJsonAsync(
            "/api/question-categories", new { nameSk = name, nameEn = name, questionCount = 0, questionsJson = "nope" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("QuestionImport.InvalidJson", await ProblemResponse.CodeOf(response));
        var categories = await _client.GetFromJsonAsync<List<QuestionCategoryDto>>("/api/question-categories");
        Assert.DoesNotContain(categories!, category => category.NameSk == name);
    }

    [Fact]
    public async Task PostImportedQuestions_IsForAdminsOnly()
    {
        var member = _factory.CreateClientAs(_factory.Member);

        var response = await member.PostAsJsonAsync("/api/question-categories/1/imported-questions", new { json = Questions });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<CreatedQuestionCategoryDto> CreateCategoryAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/question-categories",
            new { nameSk = $"Cat{Guid.NewGuid():N}"[..12], nameEn = $"Cat{Guid.NewGuid():N}"[..12], questionCount = 0 });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CreatedQuestionCategoryDto>())!;
    }
}
