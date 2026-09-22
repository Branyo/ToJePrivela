using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ToJePrivela.Infrastructure.Persistence;

/// <summary>Lets the EF tooling run without booting the API.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ToJePrivelaDbContext>
{
    public ToJePrivelaDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable($"ConnectionStrings__{SqliteConnectionString.Name}")
            ?? SqliteConnectionString.DesignTimeDefault;

        var options = new DbContextOptionsBuilder<ToJePrivelaDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new ToJePrivelaDbContext(options);
    }
}
