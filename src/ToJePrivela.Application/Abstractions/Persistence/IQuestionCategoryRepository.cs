using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IQuestionCategoryRepository : IRepository<QuestionCategory>
{
    Task<QuestionCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
