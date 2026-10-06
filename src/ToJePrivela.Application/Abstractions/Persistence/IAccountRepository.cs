using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IAccountRepository : IRepository<Account>
{
    /// <param name="name">Compared by <see cref="Account.NameKey"/>: case-insensitively, accented letters included.</param>
    Task<Account?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>The account with <see cref="Account.ReservedId"/> while it is still <see cref="Account.IsReserved"/>.</summary>
    Task<Account?> GetReservedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetAdminsAsync(CancellationToken cancellationToken = default);
}
