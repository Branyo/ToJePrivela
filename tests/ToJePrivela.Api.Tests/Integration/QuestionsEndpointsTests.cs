using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class QuestionsEndpointsTests : IClassFixture<ApiFactory>
{
    private const string ValidText = "Which year was ChatGPT publicly released?";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public QuestionsEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostQuestion_StoresTheQuestionAndDerivesBadPoints()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new
        {
            text = ValidText,
            answer = "2022",
            category = "History",
            difficulty = 2
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var question = await response.Content.ReadFromJsonAsync<QuestionDto>();
        Assert.Equal(4, question!.BadPoints);
    }

    [Fact]
    public async Task PostQuestion_RejectsANonNumericAnswer()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new
        {
            text = ValidText,
            answer = "two thousand",
            category = "History",
            difficulty = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostQuestion_RejectsDifficultyOutOfRange()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new
        {
            text = ValidText,
            answer = "2022",
            category = "History",
            difficulty = 9
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetQuestions_FiltersByCategoryAndDifficulty()
    {
        await CreateQuestionAsync("How many wheels does a car have?", "4", "Cars", 5);
        await CreateQuestionAsync("How many players are on a football pitch?", "11", "Sport", 1);

        var filtered = await _client.GetFromJsonAsync<List<QuestionDto>>("/api/questions?category=Cars&difficulty=5");

        Assert.NotEmpty(filtered!);
        Assert.All(filtered!, question =>
        {
            Assert.Equal("Cars", question.Category);
            Assert.Equal(5, question.Difficulty);
        });
    }

    [Fact]
    public async Task PutQuestion_ReplacesTheQuestion()
    {
        var question = await CreateQuestionAsync(ValidText, "2022", "History", 3);

        var response = await _client.PutAsJsonAsync($"/api/questions/{question.Id}", new
        {
            text = "In which year did the Berlin Wall fall?",
            answer = "1989",
            category = "History",
            difficulty = 1
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await _client.GetFromJsonAsync<QuestionDto>($"/api/questions/{question.Id}");
        Assert.Equal("1989", updated!.Answer);
        Assert.Equal(5, updated.BadPoints);
    }

    [Fact]
    public async Task DeleteQuestion_RemovesTheQuestion()
    {
        var question = await CreateQuestionAsync(ValidText, "2022", "History", 3);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/questions/{question.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/questions/{question.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetAiQuestions_ReturnsWhatTheGeneratorProduced()
    {
        _factory.QuestionGenerator
            .GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([new GeneratedQuestion(ValidText, "2022", "History", 3)]);

        var questions = await _client.GetFromJsonAsync<List<GeneratedQuestionDto>>(
            "/api/questions/ai?category=History&count=1");

        var question = Assert.Single(questions!);
        Assert.Equal("2022", question.Answer);
        Assert.Equal(3, question.BadPoints);
    }

    [Fact]
    public async Task GetAiQuestions_RejectsAnUnknownCategory()
    {
        var response = await _client.GetAsync("/api/questions/ai?category=Aliens&count=1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAiQuestions_ReportsUnavailableWhenTheGeneratorReturnsNothing()
    {
        _factory.QuestionGenerator
            .GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var response = await _client.GetAsync("/api/questions/ai?category=Sport&count=1");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task GetAiQuestions_RejectsATooLargeCount()
    {
        var response = await _client.GetAsync("/api/questions/ai?category=Sport&count=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<QuestionDto> CreateQuestionAsync(string text, string answer, string category, int difficulty)
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new { text, answer, category, difficulty });
        return (await response.Content.ReadFromJsonAsync<QuestionDto>())!;
    }
}
