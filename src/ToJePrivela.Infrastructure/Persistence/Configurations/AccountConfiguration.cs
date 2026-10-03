using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    private const int ProviderMaxLength = 16;

    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(ProviderMaxLength);

        builder.Property(a => a.ExternalId).HasMaxLength(Account.ExternalIdMaxLength);
        builder.Property(a => a.Email).HasMaxLength(Account.EmailMaxLength);
        builder.Property(a => a.DisplayName).IsRequired().HasMaxLength(Account.DisplayNameMaxLength);
        builder.Property(a => a.IsAdmin).HasDefaultValue(false);
        builder.Property(a => a.LastSignedInAt);

        builder.Ignore(a => a.IsBound);
        builder.Ignore(a => a.IsReserved);

        // One account per identity; SQLite lets the many unbound (null) ids through.
        builder.HasIndex(a => new { a.Provider, a.ExternalId }).IsUnique();
        builder.HasIndex(a => new { a.Provider, a.Email });
    }
}
