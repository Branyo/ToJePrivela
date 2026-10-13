using ToJePrivela.Application.Abstractions.Persistence;

namespace ToJePrivela.Application.Common;

/// <summary>
/// Makes a change and saves it; when another request changed the same rows in between (two devices tapping at once),
/// the change is not lost and not turned into a bare conflict: it is made again on freshly read rows. Gives up after
/// <see cref="MaxAttempts"/> tries, when the <see cref="ConcurrencyConflictException"/> reaches the client.
/// </summary>
public static class ConcurrencyRetry
{
    /// <summary>How often a change is made on freshly read rows when other requests keep changing them.</summary>
    public const int MaxAttempts = 3;

    /// <param name="attempt">
    /// Reads what it needs, checks and changes it, and returns the outcome. A failure is returned unsaved; a success
    /// is saved and returned.
    /// </param>
    public static async Task<TResult> SaveAsync<TResult>(
        IUnitOfWork unitOfWork,
        Func<Task<TResult>> attempt,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        for (var attemptNumber = 1; ; attemptNumber++)
        {
            var result = await attempt();

            if (result.IsFailure)
            {
                return result;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return result;
            }
            catch (ConcurrencyConflictException) when (attemptNumber < MaxAttempts)
            {
                // Forget this attempt; the next one reads the rows as the other request left them.
                unitOfWork.DiscardChanges();
            }
        }
    }
}
