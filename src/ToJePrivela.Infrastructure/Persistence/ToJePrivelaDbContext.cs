using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence.Seed;

namespace ToJePrivela.Infrastructure.Persistence;

public class ToJePrivelaDbContext : DbContext
{
    public ToJePrivelaDbContext(DbContextOptions<ToJePrivelaDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Game> Games => Set<Game>();

    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionCategory> QuestionCategories => Set<QuestionCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ToJePrivelaDbContext).Assembly);
        modelBuilder.ApplySeedData();
    }
}
