using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class QuestionRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public QuestionRepositoryTests()
    {
        using var context = _database.CreateContext();

        context.Questions.AddRange(
            new Question("Which year was ChatGPT publicly released?", "2022", "History", 3),
            new Question("In which year did the Berlin Wall fall?", "1989", "History", 1),
            new Question("How many players are on a football pitch?", "11", "Sport", 3));

        context.SaveChanges();
    }

    [Fact]
    public async Task FindAsync_WithoutFilterReturnsEverything()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        Assert.Equal(3, (await sut.FindAsync(null, null)).Count);
    }

    [Theory]
    [InlineData("History")]
    [InlineData("history")]
    [InlineData(" History ")]
    public async Task FindAsync_FiltersByCategoryIgnoringCase(string category)
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var questions = await sut.FindAsync(category, null);

        Assert.Equal(2, questions.Count);
        Assert.All(questions, question => Assert.Equal("History", question.Category));
    }

    [Fact]
    public async Task FindAsync_FiltersByDifficulty()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var questions = await sut.FindAsync(null, 3);

        Assert.Equal(2, questions.Count);
        Assert.All(questions, question => Assert.Equal(3, question.Difficulty));
    }

    [Fact]
    public async Task FindAsync_CombinesBothFilters()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var question = Assert.Single(await sut.FindAsync("History", 1));

        Assert.Equal("1989", question.Answer);
    }

    [Fact]
    public async Task FindAsync_ReturnsNothingWhenNothingMatches()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        Assert.Empty(await sut.FindAsync("Cars", 5));
    }

    [Fact]
    public async Task AddRangeAsync_StoresEveryQuestion()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        await sut.AddRangeAsync([
            new Question("How many wheels does a car have?", "4", "Cars", 5),
            new Question("How many strings does a guitar have?", "6", "Music", 4)
        ]);
        await context.SaveChangesAsync();

        Assert.Equal(5, (await sut.GetAllAsync()).Count);
    }

    [Fact]
    public async Task BadPoints_AreDerivedAndNotStored()
    {
        await using var context = _database.CreateContext();
        var sut = new QuestionRepository(context);

        var question = Assert.Single(await sut.FindAsync("Sport", null));

        Assert.Equal(3, question.BadPoints);
    }

    public void Dispose() => _database.Dispose();
}
