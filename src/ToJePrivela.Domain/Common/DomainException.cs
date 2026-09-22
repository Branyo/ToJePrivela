namespace ToJePrivela.Domain.Common;

/// <summary>Thrown when an operation would leave an entity in an invalid state.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
