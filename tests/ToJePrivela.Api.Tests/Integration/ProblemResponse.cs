using System.Net.Http.Json;
using System.Text.Json;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Reads the <c>code</c> every error answer carries.</summary>
public static class ProblemResponse
{
    public static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
