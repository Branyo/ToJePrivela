using Microsoft.Net.Http.Headers;
using ToJePrivela.Application.Abstractions.Localization;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Api.Common;

/// <summary>
/// Reads the language from the request's <c>Accept-Language</c> header: the supported language the client prefers most
/// ("en", "en-GB", "sk;q=0.8"), Slovak when it names none.
/// </summary>
public sealed class HttpCurrentLanguage : ICurrentLanguage
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentLanguage(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Language Language
    {
        get
        {
            var acceptLanguage = _httpContextAccessor.HttpContext?.Request.GetTypedHeaders().AcceptLanguage;

            if (acceptLanguage is null)
            {
                return Language.Sk;
            }

            foreach (var value in acceptLanguage.Where(value => value.Quality is null or > 0).OrderByDescending(value => value.Quality ?? 1))
            {
                if (Parse(value) is { } language)
                {
                    return language;
                }
            }

            return Language.Sk;
        }
    }

    private static Language? Parse(StringWithQualityHeaderValue value)
    {
        var tag = value.Value.Value;

        if (string.IsNullOrEmpty(tag))
        {
            return null;
        }

        var primary = tag.Split('-')[0];

        foreach (var language in Enum.GetValues<Language>())
        {
            if (string.Equals(primary, language.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        return null;
    }
}
