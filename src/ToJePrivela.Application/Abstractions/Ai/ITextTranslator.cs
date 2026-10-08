using ToJePrivela.Domain.Common;

namespace ToJePrivela.Application.Abstractions.Ai;

/// <summary>
/// Port implemented by the AI layer: translates a short text, such as a category name. Each call is a single call to
/// the provider; deciding whether the translation is usable belongs to the caller.
/// </summary>
public interface ITextTranslator
{
    /// <returns>The translation, trimmed; null when the provider answered without a usable one.</returns>
    /// <exception cref="QuestionGeneratorUnavailableException">The provider cannot be used at all.</exception>
    Task<string?> TranslateAsync(string text, Language from, Language to, CancellationToken cancellationToken = default);
}
