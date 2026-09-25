using ToJePrivela.Application.Questions;

namespace ToJePrivela.Application.Tests.Common;

public sealed class FixedBadPointsPicker : IBadPointsPicker
{
    private readonly int _badPoints;

    public FixedBadPointsPicker(int badPoints)
    {
        _badPoints = badPoints;
    }

    public int Pick() => _badPoints;
}
