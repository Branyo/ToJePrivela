using ToJePrivela.Domain.Common;

namespace ToJePrivela.Application.Abstractions.Localization;

/// <summary>
/// The language the current request wants category names and question texts in; use cases return them ready to show.
/// </summary>
public interface ICurrentLanguage
{
    /// <summary><see cref="Language.Sk"/> unless the request asks for another supported language.</summary>
    Language Language { get; }
}
