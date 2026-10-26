namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>
/// What an admin's list of logins shows of one: read on its own, so listing them never loads a password hash or a
/// security stamp.
/// </summary>
public sealed record LoginSummary(int Id, string Name, bool IsAdmin, DateTime CreatedAt, DateTime? LastSignedInAt);
