namespace ToJePrivela.Application.Abstractions.Identity;

/// <summary>The signed-in account making the current request, as its access token states it.</summary>
public interface ICurrentAccount
{
    /// <exception cref="InvalidOperationException">Nobody is signed in; the host lets no such request reach a use case.</exception>
    int Id { get; }
}
