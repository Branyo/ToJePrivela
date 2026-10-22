using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class QuestionViewConfiguration : IEntityTypeConfiguration<QuestionView>
{
    public void Configure(EntityTypeBuilder<QuestionView> builder)
    {
        // One row per login and question; the key is what the upsert in QuestionRepository conflicts on.
        builder.HasKey(v => new { v.AccountId, v.QuestionId });

        builder.Property(v => v.Count).IsRequired();
        builder.Property(v => v.LastViewedAt).IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(v => v.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // A deleted question (or category) takes its views along.
        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(v => v.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
