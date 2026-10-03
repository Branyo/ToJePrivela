using System.Text.Json.Serialization;

namespace ToJePrivela.Identity.Facebook.Contracts;

/// <summary>The answer of <c>GET /debug_token</c>.</summary>
internal sealed record DebugTokenResponse([property: JsonPropertyName("data")] DebugTokenData? Data);

internal sealed record DebugTokenData(
    [property: JsonPropertyName("app_id")] string? AppId,
    [property: JsonPropertyName("is_valid")] bool IsValid,
    [property: JsonPropertyName("user_id")] string? UserId);

/// <summary>The answer of <c>GET /me?fields=id,name,email</c>; Facebook only returns a confirmed email.</summary>
internal sealed record MeResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("email")] string? Email);
