using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class QuestionRepository : Repository<Question>, IQuestionRepository
{
    public QuestionRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public override async Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Set.Include(q => q.Category).FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Question>> FindAsync(int categoryId, QuestionSource? source, CancellationToken cancellationToken = default)
    {
        var query = Set.Include(q => q.Category).Where(q => q.CategoryId == categoryId);

        if (source is QuestionSource value)
        {
            query = query.Where(q => q.Source == value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetAiQuestionsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        await Set.Where(q => q.CategoryId == categoryId && q.Source == QuestionSource.Ai).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetTextsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        await Set.Where(q => q.CategoryId == categoryId)
            .OrderByDescending(q => q.Id)
            .Select(q => q.TextSk)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The account's view count of each candidate and the lowest of them are subqueries of the same statement (found
    /// through the views' key), and SQLite shuffles the candidates itself, so no list of ids travels to the client
    /// however many questions share that count.
    /// </summary>
    public async Task<Question?> GetRandomLeastViewedAsync(
        int accountId,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var views = Context.QuestionViews.Where(v => v.AccountId == accountId);
        var candidates = (categoryIds.Count == 0 ? Set : Set.Where(q => categoryIds.Contains(q.CategoryId)))
            .Select(q => new
            {
                Question = q,
                Views = views.Where(v => v.QuestionId == q.Id).Select(v => v.Count).FirstOrDefault()
            });

        return await candidates
            .Where(c => c.Views == candidates.Min(other => other.Views))
            .OrderBy(_ => EF.Functions.Random())
            .Select(c => c.Question)
            .Include(q => q.Category)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// SQLite's upsert: the first view inserts the row, every later one adds to it, in one statement either way. The
    /// insert reads nothing when the question does not exist, so no row changes and no foreign key is broken.
    /// </summary>
    public async Task<bool> RecordViewAsync(
        int accountId,
        int questionId,
        DateTime viewedAt,
        CancellationToken cancellationToken = default)
    {
        var at = UtcTime.Normalize(viewedAt);

        var changed = await Context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "QuestionViews" ("AccountId", "QuestionId", "Count", "LastViewedAt")
            SELECT {accountId}, {questionId}, 1, {at}
            WHERE EXISTS (SELECT 1 FROM "Questions" WHERE "Id" = {questionId})
            ON CONFLICT ("AccountId", "QuestionId")
            DO UPDATE SET "Count" = "Count" + 1, "LastViewedAt" = excluded."LastViewedAt"
            """,
            cancellationToken);

        return changed > 0;
    }

    public async Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(questions, cancellationToken);

    public void RemoveRange(IEnumerable<Question> questions) => Set.RemoveRange(questions);
}
