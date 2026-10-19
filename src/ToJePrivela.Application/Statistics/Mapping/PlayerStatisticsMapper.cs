using ToJePrivela.Application.Players.Mapping;
using ToJePrivela.Application.Statistics.Dtos;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Domain.Statistics;

namespace ToJePrivela.Application.Statistics.Mapping;

public static class PlayerStatisticsMapper
{
    public static PlayerStatisticsDto ToDto(Player player, PlayerStatistics statistics) => new(
        PlayerMapper.ToDto(player),
        statistics.GamesPlayed,
        statistics.BadCards,
        statistics.AverageBadCards,
        statistics.FinalBadPoints,
        statistics.AverageFinalBadPoints,
        statistics.Doubles,
        statistics.AverageDoubles,
        statistics.AveragePlayers,
        statistics.Losses,
        statistics.SharedLosses,
        statistics.LossIndex,
        statistics.FlawlessVictories,
        statistics.LuckyFinishes,
        statistics.ClosestEscapes,
        statistics.Reversals,
        ToDto(statistics.WorstGame),
        ToDto(statistics.BestGame),
        statistics.GamesWithCalls,
        statistics.TooMuchCalls,
        statistics.AverageTooMuchCalls,
        statistics.CorrectCalls,
        statistics.FalseAlarms,
        statistics.CallAccuracy,
        statistics.Caught,
        statistics.AverageCaught,
        statistics.CalledOn,
        statistics.AverageCalledOn,
        statistics.CaughtCardShare,
        statistics.FalseAlarmCardShare,
        statistics.CriedWolfLosses,
        ToDto(statistics.MostCorrectCalls));

    private static GameScoreDto? ToDto(GameScore? score) =>
        score is null ? null : new(score.GameId, score.Finished, score.FinalBadPoints);

    private static GameTallyDto? ToDto(GameTally? tally) =>
        tally is null ? null : new(tally.GameId, tally.Finished, tally.Count);
}
