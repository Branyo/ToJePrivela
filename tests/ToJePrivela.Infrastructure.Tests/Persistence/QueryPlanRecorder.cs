using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>
/// Asks SQLite how it runs every query a context sends (<c>EXPLAIN QUERY PLAN</c>, with the query's own parameters), so a
/// test can tell an index lookup from a scan of a whole table.
/// </summary>
public sealed class QueryPlanRecorder : DbCommandInterceptor
{
    private readonly List<QueryPlan> _plans = [];

    public IReadOnlyList<QueryPlan> Plans => _plans;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return ValueTask.FromResult(result);
    }

    private void Record(DbCommand command)
    {
        using var explain = ((SqliteConnection)command.Connection!).CreateCommand();
        explain.CommandText = "EXPLAIN QUERY PLAN " + command.CommandText;

        foreach (SqliteParameter parameter in command.Parameters)
        {
            explain.Parameters.AddWithValue(parameter.ParameterName, parameter.Value);
        }

        var steps = new List<string>();
        using (var reader = explain.ExecuteReader())
        {
            while (reader.Read())
            {
                steps.Add(reader.GetString(3));
            }
        }

        _plans.Add(new QueryPlan(command.CommandText, steps));
    }
}

/// <param name="Steps">The "detail" column of each plan row, e.g. <c>SEARCH g USING INDEX …</c> or <c>SCAN g0</c>.</param>
public sealed record QueryPlan(string Sql, IReadOnlyList<string> Steps)
{
    public override string ToString() => $"{Sql}\n  => {string.Join("\n  => ", Steps)}";
}
