using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        // A seat of its own: a deleted player's seat keeps its scores with PlayerId null, and one game may
        // hold several of those, so (GameId, PlayerId) cannot be the key. It stays unique for known players.
        builder.HasKey(gp => gp.Id);
        builder.HasIndex(gp => new { gp.GameId, gp.PlayerId }).IsUnique();

        builder.Property(gp => gp.BadPoints).HasDefaultValue(0);
        builder.Property(gp => gp.BadCards).HasDefaultValue(0);
        builder.Property(gp => gp.Doubles).HasDefaultValue(0);
        builder.Ignore(gp => gp.FinalBadPoints);
        builder.Ignore(gp => gp.IsUnknownPlayer);

        builder.HasOne(gp => gp.Game)
            .WithMany(g => g.GamePlayers)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gp => gp.Player)
            .WithMany(p => p.GamePlayers)
            .HasForeignKey(gp => gp.PlayerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
