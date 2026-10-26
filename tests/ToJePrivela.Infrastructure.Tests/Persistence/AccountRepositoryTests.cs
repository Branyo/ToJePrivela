using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class AccountRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 6, 18, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task Migrations_InsertTheReservedAccount()
    {
        await using var context = _database.CreateContext();

        var reserved = await new AccountRepository(context).GetReservedAsync();

        Assert.NotNull(reserved);
        Assert.Equal(Account.ReservedId, reserved!.Id);
        Assert.True(reserved.IsReserved);
        Assert.False(reserved.IsAdmin);
    }

    [Fact]
    public async Task GetReservedAsync_FindsNothingOnceAnAdminTookTheReservedAccount()
    {
        await using (var context = _database.CreateContext())
        {
            var reserved = await new AccountRepository(context).GetReservedAsync();
            reserved!.TakeOverReserved("Brano", "hash", Now);
            await context.SaveChangesAsync();
        }

        await using var fresh = _database.CreateContext();
        var repository = new AccountRepository(fresh);

        Assert.Null(await repository.GetReservedAsync());
        Assert.Equal(Account.ReservedId, (await repository.GetByNameAsync("brano"))!.Id);
    }

    [Theory]
    [InlineData("Brano")]
    [InlineData("BRANO")]
    [InlineData("  brano ")]
    public async Task GetByNameAsync_IgnoresCaseAndSurroundingSpaces(string name)
    {
        await StoreAsync(Account.Register("Brano", "hash", Now));

        await using var context = _database.CreateContext();

        Assert.Equal("Brano", (await new AccountRepository(context).GetByNameAsync(name))!.Name);
    }

    [Fact]
    public async Task GetByNameAsync_FoldsAccentedLetters()
    {
        await StoreAsync(Account.Register("Štefan", "hash", Now));

        await using var context = _database.CreateContext();
        var repository = new AccountRepository(context);

        Assert.NotNull(await repository.GetByNameAsync("štefan"));
        Assert.Null(await repository.GetByNameAsync("Stefan"));
    }

    [Fact]
    public async Task GetByNameAsync_NeverFindsTheReservedAccount()
    {
        await using var context = _database.CreateContext();

        Assert.Null(await new AccountRepository(context).GetByNameAsync("   "));
    }

    [Fact]
    public async Task GetAdminsAsync_ReturnsOnlyAdmins()
    {
        await StoreAsync(Account.CreateAdmin("Brano", "hash", Now));
        await StoreAsync(Account.Register("Duri", "hash", Now));

        await using var context = _database.CreateContext();

        Assert.Equal(["Brano"], (await new AccountRepository(context).GetAdminsAsync()).Select(a => a.Name));
    }

    [Fact]
    public async Task SaveChanges_RejectsASecondLoginWithTheSameName()
    {
        await StoreAsync(Account.Register("Štefan", "hash", Now));

        await using var context = _database.CreateContext();
        await new AccountRepository(context).AddAsync(Account.Register("ŠTEFAN", "hash", Now));

        await Assert.ThrowsAsync<UniqueConstraintException>(() => new UnitOfWork(context).SaveChangesAsync());
    }

    [Fact]
    public async Task StoredTimes_ComeBackAsUtc()
    {
        var account = Account.Register("Brano", "hash", Now);
        account.RecordSignIn(Now.AddMinutes(5));
        await StoreAsync(account);

        await using var context = _database.CreateContext();
        var stored = (await new AccountRepository(context).GetByNameAsync("Brano"))!;

        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
        Assert.Equal(Now.AddMinutes(5), stored.LastSignedInAt);
    }

    [Fact]
    public async Task GetLoginsAsync_PutsTheMostRecentlySignedInFirstAndThoseNeverSignedInLast()
    {
        var early = Account.Register("Early", "hash", Now);
        early.RecordSignIn(Now.AddHours(1));
        var late = Account.Register("Late", "hash", Now);
        late.RecordSignIn(Now.AddHours(2));
        await StoreAsync(Account.CreateAdmin("Never old", "hash", Now));
        await StoreAsync(early);
        await StoreAsync(Account.CreateAdmin("Never new", "hash", Now.AddDays(1)));
        await StoreAsync(late);

        await using var context = _database.CreateContext();

        Assert.Equal(
            ["Late", "Early", "Never new", "Never old"],
            (await new AccountRepository(context).GetLoginsAsync()).Select(a => a.Name));
    }

    [Fact]
    public async Task GetLoginsAsync_GetLoginAsync_AndLoginExistsAsync_LeaveOutTheReservedAccount()
    {
        await using var context = _database.CreateContext();
        var repository = new AccountRepository(context);

        Assert.Empty(await repository.GetLoginsAsync());
        Assert.Null(await repository.GetLoginAsync(Account.ReservedId));
        Assert.False(await repository.LoginExistsAsync(Account.ReservedId));
    }

    [Fact]
    public async Task GetLoginAsync_ReadsTheLoginWithoutTrackingIt()
    {
        var login = Account.Register("Duri", "hash", Now);
        await StoreAsync(login);

        await using var context = _database.CreateContext();
        var read = await new AccountRepository(context).GetLoginAsync(login.Id);

        Assert.Equal("Duri", read?.Name);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task DeleteLoginAsync_DeletesTheLoginWithEverythingItOwnsAndNothingElse()
    {
        var doomed = Account.Register("Doomed", "hash", Now);
        var kept = Account.Register("Kept", "hash", Now);
        await StoreAsync(doomed);
        await StoreAsync(kept);
        int questionId;

        await using (var context = _database.CreateContext())
        {
            var question = new Question("Koľko nôh má pavúk?", "How many legs does a spider have?", "8", context.QuestionCategories.Find(1)!, 3, QuestionSource.Manual, Now);
            context.Questions.Add(question);
            foreach (var accountId in new[] { doomed.Id, kept.Id })
            {
                var anna = new Player(accountId, "Anna", "🦊");
                var boris = new Player(accountId, "Boris", "🐻");
                context.Players.AddRange(anna, boris);
                await context.SaveChangesAsync();
                context.Games.Add(new Game(accountId, [anna.Id, boris.Id], Now));
            }

            await context.SaveChangesAsync();
            questionId = question.Id;
        }

        await using (var context = _database.CreateContext())
        {
            var questions = new QuestionRepository(context);
            await questions.RecordViewAsync(doomed.Id, questionId, Now);
            await questions.RecordViewAsync(kept.Id, questionId, Now);
        }

        await using (var context = _database.CreateContext())
        {
            Assert.True(await new AccountRepository(context).DeleteLoginAsync(doomed.Id));
        }

        await using var fresh = _database.CreateContext();
        Assert.Null(await fresh.Accounts.FindAsync(doomed.Id));
        Assert.NotNull(await fresh.Accounts.FindAsync(kept.Id));
        Assert.Equal((0, 2), (await fresh.Players.CountAsync(p => p.AccountId == doomed.Id), await fresh.Players.CountAsync(p => p.AccountId == kept.Id)));
        Assert.Equal(kept.Id, (await fresh.Games.SingleAsync()).AccountId);
        Assert.Equal(2, await fresh.GamePlayers.CountAsync());
        Assert.Equal(kept.Id, (await fresh.QuestionViews.SingleAsync()).AccountId);
    }

    [Fact]
    public async Task DeleteLoginAsync_FindsNoUnknownLoginAndNeverTheReservedAccount()
    {
        await using var context = _database.CreateContext();
        var repository = new AccountRepository(context);

        Assert.False(await repository.DeleteLoginAsync(999));
        Assert.False(await repository.DeleteLoginAsync(Account.ReservedId));
        Assert.NotNull(await repository.GetReservedAsync());
    }

    private async Task StoreAsync(Account account)
    {
        await using var context = _database.CreateContext();
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _database.Dispose();
}
