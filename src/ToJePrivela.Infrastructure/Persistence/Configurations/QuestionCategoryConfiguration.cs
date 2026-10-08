using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class QuestionCategoryConfiguration : IEntityTypeConfiguration<QuestionCategory>
{
    public void Configure(EntityTypeBuilder<QuestionCategory> builder)
    {
        builder.HasKey(qc => qc.Id);

        builder.Property(qc => qc.NameSk)
            .IsRequired()
            .HasMaxLength(QuestionCategory.NameMaxLength)
            .UseCollation("NOCASE");

        builder.Property(qc => qc.NameEn)
            .IsRequired()
            .HasMaxLength(QuestionCategory.NameMaxLength)
            .UseCollation("NOCASE");

        // NOCASE folds ASCII only ("Š" and "š" differ), so uniqueness rests on the Unicode-aware keys, one per language.
        builder.Property(qc => qc.NameSkKey)
            .IsRequired()
            .HasMaxLength(QuestionCategory.NameMaxLength);

        builder.Property(qc => qc.NameEnKey)
            .IsRequired()
            .HasMaxLength(QuestionCategory.NameMaxLength);

        builder.HasIndex(qc => qc.NameSkKey).IsUnique();
        builder.HasIndex(qc => qc.NameEnKey).IsUnique();
    }
}
