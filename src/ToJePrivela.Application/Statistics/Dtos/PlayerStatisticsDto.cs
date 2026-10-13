using ToJePrivela.Application.Players.Dtos;

namespace ToJePrivela.Application.Statistics.Dtos;

/// <summary>
/// A player's record over their finished, not cancelled games. Averages are per counted game and, like
/// <see cref="LossIndex"/>, null when there is nothing to average; <see cref="WorstGame"/> and <see cref="BestGame"/>
/// are null without a counted game.
/// </summary>
/// <param name="LossIndex">Actual loss rate over the expected one (average of losers / players per game); 1 is average.</param>
public sealed record PlayerStatisticsDto(
    PlayerDto Player,
    int GamesPlayed,
    int BadCards,
    double? AverageBadCards,
    int FinalBadPoints,
    double? AverageFinalBadPoints,
    int Doubles,
    double? AverageDoubles,
    double? AveragePlayers,
    int Losses,
    int SharedLosses,
    double? LossIndex,
    int FlawlessVictories,
    int LuckyFinishes,
    int ClosestEscapes,
    int Reversals,
    GameScoreDto? WorstGame,
    GameScoreDto? BestGame);

/// <summary>The player's final bad points in one game, with the game to link to.</summary>
public sealed record GameScoreDto(int GameId, DateTime Finished, int FinalBadPoints);
