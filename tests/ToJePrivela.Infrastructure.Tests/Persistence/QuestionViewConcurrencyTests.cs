using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Questions;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>The view use case against a real database, where another request wins the race.</summary>
public class QuestionViewConcurrencyTests : IDisposable
{
    private const int HistoryId = 3;

    private readonly SqliteTestDatabase _database = new();
    private readonly int _questionId;

    public QuestionViewConcurrencyTests()
    {
        using var context = _database.CreateContext();

        var question = new Question(
            "In which year did the Berlin Wall fall?", "1989", context.QuestionCategories.Find(HistoryId)!, 3,
            QuestionSource.Manual, DateTime.UtcNow);
        context.Questions.Add(question);
        context.SaveChanges();

        _questionId = question.Id;
    }

    [Fact]
    public async Task RecordViewAsync_KeepsTheConcurrentViewAndAddsItsOwn()
    {
        await using var context = _database.CreateContext();

        // The request reads the question, then another request counts a view before this one saves.
        await context.Questions.FirstAsync(q => q.Id == _questionId);
        await ViewInAnotherRequestAsync();

        var result = await CreateService(context).RecordViewAsync(_questionId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.ViewCount);

        await using var verification = _database.CreateContext();
        Assert.Equal(2, (await verification.Questions.FindAsync(_questionId))!.ViewCount);
    }

    private async Task ViewInAnotherRequestAsync()
    {
        await using var other = _database.CreateContext();
        (await other.Questions.FindAsync(_questionId))!.MarkViewed(DateTime.UtcNow);
        await other.SaveChangesAsync();
    }

    private static QuestionService CreateService(ToJePrivelaDbContext context) => new(
        new QuestionRepository(context),
        new QuestionCategoryRepository(context),
        new RandomBadPointsPicker(),
        new RandomQuestionPicker(),
        TimeProvider.System,
        new UnitOfWork(context));

    public void Dispose() => _database.Dispose();
}
