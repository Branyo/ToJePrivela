using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Conversions;
using ToJePrivela.Infrastructure.Persistence.Seed;

namespace ToJePrivela.Infrastructure.Persistence;

public class ToJePrivelaDbContext : DbContext
{
    public ToJePrivelaDbContext(DbContextOptions<ToJePrivelaDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Game> Games => Set<Game>();

    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionCategory> QuestionCategories => Set<QuestionCategory>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpGameVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpGameVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ToJePrivelaDbContext).Assembly);
        modelBuilder.ApplySeedData();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <summary>
    /// <see cref="Game.Version"/> is bumped here, once per save, for every stored game that changed itself or in one of
    /// its players, so no domain method can forget it. Counting from the version read keeps a retried save from
    /// bumping twice, and the update is still guarded by that read version.
    /// </summary>
    private void BumpGameVersions()
    {
        ChangeTracker.DetectChanges();

        foreach (var game in ChangeTracker.Entries<Game>())
        {
            var changed = game.State == EntityState.Modified
                || (game.State == EntityState.Unchanged && game.Entity.GamePlayers.Any(gp =>
                    Entry(gp).State is EntityState.Modified or EntityState.Added or EntityState.Deleted));

            if (changed)
            {
                var version = game.Property(g => g.Version);
                version.CurrentValue = version.OriginalValue + 1;
            }
        }
    }
}
