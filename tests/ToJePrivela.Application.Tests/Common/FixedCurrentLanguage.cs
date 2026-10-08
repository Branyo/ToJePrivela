using ToJePrivela.Application.Abstractions.Localization;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Application.Tests.Common;

public sealed class FixedCurrentLanguage : ICurrentLanguage
{
    public FixedCurrentLanguage(Language language = Language.Sk)
    {
        Language = language;
    }

    public Language Language { get; set; }
}
