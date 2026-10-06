using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Identity;

/// <summary>Issues the token the client sends with every request after signing in.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(Account account);
}

public sealed record AccessToken(string Value, DateTime ExpiresAt);
