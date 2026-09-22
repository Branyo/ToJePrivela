using Microsoft.Extensions.Configuration;

namespace ToJePrivela.Infrastructure.Persistence;

/// <summary>Single place that knows how the SQLite connection string is named and validated.</summary>
public static class SqliteConnectionString
{
    public const string Name = "ToJePrivelaDbConnectionString";

    public const string DesignTimeDefault = "Data Source=ToJePrivela.db";

    public static string Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(Name);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{Name}' is missing or empty. Set it in appsettings.json, " +
                $"in user-secrets, or via the ConnectionStrings__{Name} environment variable.");
        }

        return connectionString;
    }
}
