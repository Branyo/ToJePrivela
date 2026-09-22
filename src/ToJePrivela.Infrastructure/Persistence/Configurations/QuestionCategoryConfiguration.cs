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

        builder.HasIndex(qc => qc.Name).IsUnique();

        builder.HasOne(qc => qc.AddedByPlayer)
            .WithMany()
            .HasForeignKey(qc => qc.AddedByPlayerId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
