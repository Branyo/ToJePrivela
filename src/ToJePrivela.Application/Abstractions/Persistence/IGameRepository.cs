using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>A game is always loaded whole, with its players, so its rules never run on half an aggregate.</summary>
public interface IGameRepository : IRepository<Game>
{
}
