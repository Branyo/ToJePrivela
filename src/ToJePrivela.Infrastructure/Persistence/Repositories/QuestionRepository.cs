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

    public override async Task<IReadOnlyList<Question>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.Include(q => q.Category).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Question>> FindAsync(int? categoryId, QuestionSource? source, CancellationToken cancellationToken = default)
    {
        var query = Set.Include(q => q.Category).AsQueryable();

        if (categoryId is int id)
        {
            query = query.Where(q => q.CategoryId == id);
        }

        if (source is QuestionSource value)
        {
            query = query.Where(q => q.Source == value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetTextsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        await Set.Where(q => q.CategoryId == categoryId)
            .OrderByDescending(q => q.Id)
            .Select(q => q.Text)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(questions, cancellationToken);

    public void RemoveRange(IEnumerable<Question> questions) => Set.RemoveRange(questions);
}
