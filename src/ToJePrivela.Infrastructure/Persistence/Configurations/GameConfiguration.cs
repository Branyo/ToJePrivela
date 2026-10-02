using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    private const int BadPointsModeMaxLength = 16;

    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Started);
        builder.Property(g => g.Finished);
        builder.Property(g => g.BadCardLimit).HasDefaultValue(Game.DefaultBadCardLimit);
        builder.Property(g => g.BadPointsMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(BadPointsModeMaxLength);
        builder.Property(g => g.IsCancelled).HasDefaultValue(false);
        builder.Ignore(g => g.IsFinished);

        builder.Metadata
            .FindNavigation(nameof(Game.GamePlayers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
