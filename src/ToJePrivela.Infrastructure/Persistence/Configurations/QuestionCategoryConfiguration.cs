using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class QuestionCategoryConfiguration : IEntityTypeConfiguration<QuestionCategory>
{
    public void Configure(EntityTypeBuilder<QuestionCategory> builder)
    {
        builder.HasKey(qc => qc.Id);

        builder.Property(qc => qc.Name)
            .IsRequired()
            .HasMaxLength(QuestionCategory.NameMaxLength)
            .UseCollation("NOCASE");

        // NOCASE folds ASCII only ("Š" and "š" differ), so uniqueness rests on the Unicode-aware key.
        builder.Property(qc => qc.NameKey)
            .IsRequired()
            .HasMaxLength(QuestionCategory.NameMaxLength);

        builder.HasIndex(qc => qc.NameKey).IsUnique();
    }
}
