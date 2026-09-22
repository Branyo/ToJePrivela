using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class QuestionRepository : Repository<Question>, IQuestionRepository
{
    public QuestionRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Question>> FindAsync(string? category, int? difficulty, CancellationToken cancellationToken = default)
    {
        var query = Set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var trimmed = category.Trim();
            query = query.Where(q => q.Category == trimmed);
        }

        if (difficulty is int value)
        {
            query = query.Where(q => q.Difficulty == value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(questions, cancellationToken);
}
