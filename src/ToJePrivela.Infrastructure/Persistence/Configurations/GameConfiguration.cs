using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Started);
        builder.Property(g => g.Finished);
        builder.Ignore(g => g.IsFinished);

        builder.Metadata
            .FindNavigation(nameof(Game.GamePlayers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
