using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(Player.NameMaxLength)
            .UseCollation("NOCASE");

        builder.HasIndex(p => p.Name).IsUnique();

        builder.Metadata
            .FindNavigation(nameof(Player.GamePlayers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
