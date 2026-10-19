using ToJePrivela.Application.Players.Dtos;

namespace ToJePrivela.Application.Statistics.Dtos;

/// <summary>
/// A player's record over their finished, not cancelled games. Averages are per counted game and, like
/// <see cref="LossIndex"/>, null when there is nothing to average; <see cref="WorstGame"/> and <see cref="BestGame"/>
/// are null without a counted game. The call figures come only from <see cref="GamesWithCalls"/>, and their averages are
/// per those games.
/// </summary>
/// <param name="LossIndex">Actual loss rate over the expected one (average of losers / players per game); 1 is average.</param>
/// <param name="CallAccuracy">Correct calls out of all calls, 0–1; null without a call.</param>
/// <param name="CalledOn">Times the next player called the player's bid too much, rightly (caught) or not.</param>
/// <param name="CaughtCardShare">Share of the cards (0–1) taken for being caught overbidding; the rest are false alarms.</param>
/// <param name="CriedWolfLosses">Losses with more false-alarm cards than cards for being caught.</param>
/// <param name="MostCorrectCalls">Most correct calls in one game; null without a correct call.</param>
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
    GameScoreDto? BestGame,
    int GamesWithCalls,
    int TooMuchCalls,
    double? AverageTooMuchCalls,
    int CorrectCalls,
    int FalseAlarms,
    double? CallAccuracy,
    int Caught,
    double? AverageCaught,
    int CalledOn,
    double? AverageCalledOn,
    double? CaughtCardShare,
    int CriedWolfLosses,
    GameTallyDto? MostCorrectCalls);

/// <summary>The player's final bad points in one game, with the game to link to.</summary>
public sealed record GameScoreDto(int GameId, DateTime Finished, int FinalBadPoints);

/// <summary>How many times something happened to the player in one game, with the game to link to.</summary>
public sealed record GameTallyDto(int GameId, DateTime Finished, int Count);
