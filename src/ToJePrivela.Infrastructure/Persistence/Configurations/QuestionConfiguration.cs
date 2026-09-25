using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public const int SourceMaxLength = 16;

    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text)
            .IsRequired()
            .HasMaxLength(Question.TextMaxLength);

        builder.Property(q => q.Answer)
            .IsRequired();

        builder.Property(q => q.BadPoints).IsRequired();

        builder.Property(q => q.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(SourceMaxLength);

        builder.Property(q => q.CreatedAt).IsRequired();

        builder.Property(q => q.ViewCount).IsRequired();

        // Updates are guarded by WHERE "Version" = <the value read>, so a lost update surfaces as a conflict.
        builder.Property(q => q.Version)
            .IsRequired()
            .IsConcurrencyToken();

        // Deleting a category deletes its questions, manual and AI alike.
        builder.HasOne(q => q.Category)
            .WithMany()
            .HasForeignKey(q => q.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => new { q.CategoryId, q.Source });
        builder.HasIndex(q => new { q.CategoryId, q.ViewCount });
    }
}
