using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>
/// Every game read finds the seats it needs through an index: none reads the whole GamePlayers table, as one query
/// joining the seats to their players in a subquery made SQLite do (it materialized every seat of every game).
/// </summary>
public class GameQueryPlanTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public static TheoryData<string> Reads => ["GetById", "GetAll", "GetByPlayer", "GetRecent", "GetRecentAfter", "GetRecentAfterUnstarted", "GetCountedByPlayer"];

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task EveryGameRead_FindsTheSeatsThroughAnIndex(string read)
    {
        var gameId = await SeedGameAsync(started: read != "GetRecentAfterUnstarted");
        var recorder = new QueryPlanRecorder();
        await using var context = _database.CreateContext(recorder);
        var sut = new GameRepository(context);

        await (read switch
        {
            "GetById" => (Task)sut.GetByIdAsync(TestAccountId, gameId),
            "GetAll" => sut.GetAllAsync(TestAccountId),
            "GetByPlayer" => sut.GetByPlayerAsync(TestAccountId, 1),
            "GetRecent" => sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, null, 10),
            "GetRecentAfter" => sut.GetRecentAsync(TestAccountId, GameHistoryStatus.Running, new GamePosition(Start.AddDays(1), 99), 10),
            "GetRecentAfterUnstarted" => sut.GetRecentAsync(TestAccountId, GameHistoryStatus.All, new GamePosition(null, gameId + 1), 10),
            "GetCountedByPlayer" => sut.GetCountedByPlayerAsync(TestAccountId, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(read)),
        });

        var seatPlans = recorder.Plans.Where(plan => plan.Sql.Contains("\"GamePlayers\"")).ToList();
        Assert.NotEmpty(seatPlans);

        foreach (var plan in seatPlans)
        {
            Assert.DoesNotContain(plan.Steps, step => step.StartsWith("MATERIALIZE", StringComparison.Ordinal));

            foreach (var alias in Regex.Matches(plan.Sql, "\"GamePlayers\" AS \"(\\w+)\"").Select(match => match.Groups[1].Value))
            {
                var steps = plan.Steps.Where(step => Regex.IsMatch(step, $"^(SCAN|SEARCH) {alias}\\b")).ToList();
                Assert.True(
                    steps.Count > 0 && steps.All(step => step.StartsWith("SEARCH", StringComparison.Ordinal)),
                    $"GamePlayers ({alias}) is not read through an index:\n{plan}");
            }
        }
    }

    /// <param name="started">False for a game without a start, as games could be before starts were stored.</param>
    private async Task<int> SeedGameAsync(bool started = true)
    {
        await using var context = _database.CreateContext();
        var game = new Game(TestAccountId, [1, 2], Start);
        context.Games.Add(game);
        await context.SaveChangesAsync();

        if (!started)
        {
            await context.Database.ExecuteSqlAsync($"UPDATE \"Games\" SET \"Started\" = NULL WHERE \"Id\" = {game.Id}");
        }

        return game.Id;
    }

    public void Dispose() => _database.Dispose();
}
