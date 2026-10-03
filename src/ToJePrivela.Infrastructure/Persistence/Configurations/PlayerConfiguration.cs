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

        // NOCASE folds ASCII only ("Š" and "š" differ), so uniqueness rests on the Unicode-aware key.
        builder.Property(p => p.NameKey)
            .IsRequired()
            .HasMaxLength(Player.NameMaxLength);

        // Names are unique per account: two logins may each have their own "Brano".
        builder.HasIndex(p => new { p.AccountId, p.NameKey }).IsUnique();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Avatar)
            .IsRequired()
            .HasMaxLength(PlayerAvatars.MaxLength);

        builder.Metadata
            .FindNavigation(nameof(Player.GamePlayers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
