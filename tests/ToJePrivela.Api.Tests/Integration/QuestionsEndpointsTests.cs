using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class QuestionsEndpointsTests : IClassFixture<ApiFactory>
{
    private const string ValidText = "Which year was ChatGPT publicly released?";
    private const int CarsId = 1;
    private const int SportId = 2;
    private const int HistoryId = 3;

    private readonly HttpClient _client;

    public QuestionsEndpointsTests(ApiFactory factory)
    {
        factory.ReplyWithFreshQuestions();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostQuestion_StoresAManualQuestionWithTheGivenBadPoints()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new
        {
            text = ValidText,
            answer = "2022",
            categoryId = HistoryId,
            badPoints = 2
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var question = await response.Content.ReadFromJsonAsync<QuestionDto>();
        Assert.Equal(2, question!.BadPoints);
        Assert.Equal(HistoryId, question.CategoryId);
        Assert.Equal("History", question.CategoryName);
        Assert.Equal("Manual", question.Source);
        Assert.NotEqual(default, question.CreatedAt);
    }

    [Fact]
    public async Task PostQuestion_PicksBadPointsWhenNoneAreGiven()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new { text = ValidText, answer = "2022", categoryId = HistoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.InRange((await response.Content.ReadFromJsonAsync<QuestionDto>())!.BadPoints, 1, 5);
    }

    [Fact]
    public async Task PostQuestion_RejectsAnUnknownCategory()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new { text = ValidText, answer = "2022", categoryId = 9999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostQuestion_RequiresACategory()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new { text = ValidText, answer = "2022" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostQuestion_RejectsANonNumericAnswer()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new { text = ValidText, answer = "two thousand", categoryId = HistoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task PostQuestion_RejectsBadPointsOutOfRange(int badPoints)
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new { text = ValidText, answer = "2022", categoryId = HistoryId, badPoints });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetQuestions_FiltersByCategoryAndSource()
    {
        await CreateQuestionAsync("How many wheels does a car have?", "4", CarsId);
        await _client.PostAsJsonAsync($"/api/question-categories/{CarsId}/ai-questions", new { count = 2 });

        var manual = await _client.GetFromJsonAsync<List<QuestionDto>>($"/api/questions?categoryId={CarsId}&source=Manual");
        var ai = await _client.GetFromJsonAsync<List<QuestionDto>>($"/api/questions?categoryId={CarsId}&source=Ai");

        Assert.NotEmpty(manual!);
        Assert.All(manual!, question => Assert.Equal(("Cars", "Manual"), (question.CategoryName, question.Source)));
        Assert.True(ai!.Count >= 2);
        Assert.All(ai, question => Assert.Equal(("Cars", "Ai"), (question.CategoryName, question.Source)));
    }

    [Fact]
    public async Task PutQuestion_ReplacesTheQuestionIncludingCategoryAndBadPoints()
    {
        var question = await CreateQuestionAsync(ValidText, "2022", HistoryId);

        var response = await _client.PutAsJsonAsync($"/api/questions/{question.Id}", new
        {
            text = "How many players are on a football pitch?",
            answer = "22",
            categoryId = SportId,
            badPoints = 5
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await _client.GetFromJsonAsync<QuestionDto>($"/api/questions/{question.Id}");
        Assert.Equal("22", updated!.Answer);
        Assert.Equal("Sport", updated.CategoryName);
        Assert.Equal(5, updated.BadPoints);
    }

    [Fact]
    public async Task PutQuestion_KeepsBadPointsWhenNoneAreGiven()
    {
        var question = await CreateQuestionAsync(ValidText, "2022", HistoryId, badPoints: 4);

        await _client.PutAsJsonAsync($"/api/questions/{question.Id}", new { text = ValidText, answer = "2023", categoryId = HistoryId });

        Assert.Equal(4, (await _client.GetFromJsonAsync<QuestionDto>($"/api/questions/{question.Id}"))!.BadPoints);
    }

    [Fact]
    public async Task PutQuestion_TurnsAnEditedAiQuestionIntoAManualOne()
    {
        var generated = await (await _client.PostAsJsonAsync($"/api/question-categories/{SportId}/ai-questions", new { count = 1 }))
            .Content.ReadFromJsonAsync<GeneratedAiQuestionsDto>();
        var aiQuestion = Assert.Single(generated!.Questions);

        await _client.PutAsJsonAsync($"/api/questions/{aiQuestion.Id}", new { text = aiQuestion.Text, answer = "7", categoryId = SportId });

        Assert.Equal("Manual", (await _client.GetFromJsonAsync<QuestionDto>($"/api/questions/{aiQuestion.Id}"))!.Source);
    }

    [Fact]
    public async Task PutQuestion_RejectsAnUnknownCategory()
    {
        var question = await CreateQuestionAsync(ValidText, "2022", HistoryId);

        var response = await _client.PutAsJsonAsync($"/api/questions/{question.Id}", new { text = ValidText, answer = "2022", categoryId = 9999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuestion_RemovesTheQuestion()
    {
        var question = await CreateQuestionAsync(ValidText, "2022", HistoryId);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/questions/{question.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/questions/{question.Id}")).StatusCode);
    }

    [Fact]
    public async Task TheOldAiEndpointIsGone()
    {
        var response = await _client.GetAsync("/api/questions/ai?category=Sport&count=1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<QuestionDto> CreateQuestionAsync(string text, string answer, int categoryId, int badPoints = 3)
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new { text, answer, categoryId, badPoints });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<QuestionDto>())!;
    }
}
