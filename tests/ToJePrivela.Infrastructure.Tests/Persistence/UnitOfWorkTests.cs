using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class UnitOfWorkTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task SaveChangesAsync_CommitsPendingWork()
    {
        await using var context = _database.CreateContext();
        var players = new PlayerRepository(context);
        var sut = new UnitOfWork(context);

        await players.AddAsync(new Player("Jozo"));
        var affected = await sut.SaveChangesAsync();

        Assert.Equal(1, affected);

        await using var verification = _database.CreateContext();
        Assert.NotNull(await verification.Players.FirstOrDefaultAsync(p => p.Name == "Jozo"));
    }

    [Fact]
    public async Task ChangesAreNotVisibleBeforeSaving()
    {
        await using var context = _database.CreateContext();
        var players = new PlayerRepository(context);

        await players.AddAsync(new Player("Jozo"));

        await using var verification = _database.CreateContext();
        Assert.Null(await verification.Players.FirstOrDefaultAsync(p => p.Name == "Jozo"));
    }

    [Fact]
    public async Task SaveChangesAsync_ReportsNoWorkWhenNothingChanged()
    {
        await using var context = _database.CreateContext();
        var sut = new UnitOfWork(context);

        Assert.Equal(0, await sut.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_ReportsAUniqueIndexViolation()
    {
        await using var context = _database.CreateContext();
        var categories = new QuestionCategoryRepository(context);
        var sut = new UnitOfWork(context);

        await categories.AddAsync(new QuestionCategory("sport"));

        var exception = await Assert.ThrowsAsync<UniqueConstraintException>(() => sut.SaveChangesAsync());
        Assert.IsType<DbUpdateException>(exception.InnerException);
    }

    [Fact]
    public async Task SaveChangesAsync_LeavesOtherFailuresAlone()
    {
        await using var context = _database.CreateContext();
        var sut = new UnitOfWork(context);

        // A question pointing at a category that does not exist breaks the foreign key, not a unique index.
        var missingCategory = new QuestionCategory("Missing");
        typeof(QuestionCategory).GetProperty(nameof(QuestionCategory.Id))!.SetValue(missingCategory, 999);
        context.Attach(missingCategory);
        context.Questions.Add(new Question(
            "How many wheels does a car have?", "4", missingCategory, 1, QuestionSource.Manual, DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => sut.SaveChangesAsync());
    }

    public void Dispose() => _database.Dispose();
}
