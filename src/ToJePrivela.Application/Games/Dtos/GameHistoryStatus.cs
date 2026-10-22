namespace ToJePrivela.Application.Games.Dtos;

/// <summary>Which games the history lists; a cancelled game counts as finished.</summary>
public enum GameHistoryStatus
{
    All,
    Running,
    Finished,
}
