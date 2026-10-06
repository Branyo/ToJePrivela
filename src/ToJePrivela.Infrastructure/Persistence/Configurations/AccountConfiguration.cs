using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(Account.NameMaxLength)
            .UseCollation("NOCASE");

        // NOCASE folds ASCII only ("Š" and "š" differ), so uniqueness rests on the Unicode-aware key. The reserved
        // account's key is empty, which no other login can have.
        builder.Property(a => a.NameKey)
            .IsRequired()
            .HasMaxLength(Account.NameMaxLength);

        builder.HasIndex(a => a.NameKey).IsUnique();

        builder.Property(a => a.PasswordHash).HasMaxLength(Account.PasswordHashMaxLength);
        builder.Property(a => a.IsAdmin).HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.LastSignedInAt);

        builder.Ignore(a => a.IsReserved);
    }
}
