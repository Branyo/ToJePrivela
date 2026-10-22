using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
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
    /// The lowest view count is a subquery of the same statement and SQLite shuffles the candidates itself, so no list
    /// of ids travels to the client however many questions share that count.
    /// </summary>
    public async Task<Question?> GetRandomLeastViewedAsync(
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var candidates = categoryIds.Count == 0 ? Set : Set.Where(q => categoryIds.Contains(q.CategoryId));

        return await candidates
            .Where(q => q.ViewCount == candidates.Min(c => c.ViewCount))
            .OrderBy(_ => EF.Functions.Random())
            .Include(q => q.Category)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(questions, cancellationToken);

    public void RemoveRange(IEnumerable<Question> questions) => Set.RemoveRange(questions);
}
