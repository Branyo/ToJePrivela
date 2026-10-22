using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Localization;
using ToJePrivela.Application.Questions;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>The view use case against a real database, where several devices of one login show a question at once.</summary>
public class QuestionViewConcurrencyTests : IDisposable
{
    private const int HistoryId = 3;
    private const int Views = 10;

    private readonly SqliteTestDatabase _database = new();
    private readonly int _questionId;
    private readonly int _accountId;

    public QuestionViewConcurrencyTests()
    {
        using var context = _database.CreateContext();

        var question = new Question(
            "V ktorom roku padol Berlínsky múr?", "In which year did the Berlin Wall fall?", "1989", context.QuestionCategories.Find(HistoryId)!, 3,
            QuestionSource.Manual, DateTime.UtcNow);
        var account = Account.Register("Party", "hash", DateTime.UtcNow);
        context.Questions.Add(question);
        context.Accounts.Add(account);
        context.SaveChanges();

        _questionId = question.Id;
        _accountId = account.Id;
    }

    [Fact]
    public async Task RecordViewAsync_CountsEveryConcurrentView()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, Views).Select(async _ =>
        {
            await using var context = _database.CreateContext();
            return await CreateService(context).RecordViewAsync(_questionId);
        }));

        Assert.All(results, result => Assert.True(result.IsSuccess));

        await using var verification = _database.CreateContext();
        var view = await verification.QuestionViews.SingleAsync();
        Assert.Equal((_accountId, _questionId, Views), (view.AccountId, view.QuestionId, view.Count));
    }

    private QuestionService CreateService(ToJePrivelaDbContext context)
    {
        var currentAccount = Substitute.For<ICurrentAccount>();
        currentAccount.Id.Returns(_accountId);

        return new(
            new QuestionRepository(context),
            new QuestionCategoryRepository(context),
            new RandomBadPointsPicker(),
            TimeProvider.System,
            Substitute.For<ICurrentLanguage>(),
            currentAccount,
            new UnitOfWork(context));
    }

    public void Dispose() => _database.Dispose();
}
