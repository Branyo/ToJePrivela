namespace ToJePrivela.Application.Common;

/// <summary>Errors for a request that breaks its own rules, whoever noticed it (the use case or ASP.NET).</summary>
public static class RequestErrors
{
    public static readonly Error Missing = Error.Validation("Request.Missing", "The request is required.");

    public static Error Invalid(IEnumerable<string> messages) =>
        Error.Validation("Request.Invalid", string.Join(" ", messages.Distinct()));
}
