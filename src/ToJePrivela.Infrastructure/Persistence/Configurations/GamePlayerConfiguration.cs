using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.HasKey(gp => new { gp.GameId, gp.PlayerId });

        builder.Property(gp => gp.BadPoints).HasDefaultValue(0);

        builder.HasOne(gp => gp.Game)
            .WithMany(g => g.GamePlayers)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gp => gp.Player)
            .WithMany(p => p.GamePlayers)
            .HasForeignKey(gp => gp.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
