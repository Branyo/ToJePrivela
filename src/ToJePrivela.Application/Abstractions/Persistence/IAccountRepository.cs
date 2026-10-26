using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IAccountRepository : IRepository<Account>
{
    /// <param name="name">Compared by <see cref="Account.NameKey"/>: case-insensitively, accented letters included.</param>
    Task<Account?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>The account with <see cref="Account.ReservedId"/> while it is still <see cref="Account.IsReserved"/>.</summary>
    Task<Account?> GetReservedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetAdminsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Every login (the reserved account is none), read only: the most recently signed in first, those never signed in
    /// last, newest first.
    /// </summary>
    Task<IReadOnlyList<LoginSummary>> GetLoginsAsync(CancellationToken cancellationToken = default);

    /// <summary>The login with the id, read only; null for the reserved account while nobody took it over.</summary>
    Task<LoginSummary?> GetLoginAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Whether a login (not the reserved account) has the id; asks the database only.</summary>
    Task<bool> LoginExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the login with everything it owns (its games with their seats, its players and its question views) in one
    /// transaction of its own, saved at once rather than by the unit of work. False when no login has the id.
    /// </summary>
    Task<bool> DeleteLoginAsync(int id, CancellationToken cancellationToken = default);
}
