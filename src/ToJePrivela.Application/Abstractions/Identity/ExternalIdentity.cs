using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Identity;

/// <summary>Who a provider vouched for.</summary>
/// <param name="ExternalId">The provider's id of the identity (Google's <c>sub</c>, Facebook's user id).</param>
/// <param name="Email">Only an address the provider verified; null otherwise.</param>
public sealed record ExternalIdentity(IdentityProvider Provider, string ExternalId, string? Email, string DisplayName);
