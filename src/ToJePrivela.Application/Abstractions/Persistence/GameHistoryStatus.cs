namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>Which games the history lists; a cancelled game counts as finished (<c>Game.FinishedGame</c>).</summary>
public enum GameHistoryStatus
{
    All,
    Running,
    Finished,
}
