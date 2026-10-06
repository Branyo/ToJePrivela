using ToJePrivela.Application.Abstractions.Identity;

namespace ToJePrivela.Application.Tests.Common;

public sealed class FixedCurrentAccount : ICurrentAccount
{
    public FixedCurrentAccount(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
