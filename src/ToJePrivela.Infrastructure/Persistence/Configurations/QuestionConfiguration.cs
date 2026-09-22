using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text)
            .IsRequired()
            .HasMaxLength(Question.TextMaxLength);

        builder.Property(q => q.Answer)
            .IsRequired();

        builder.Property(q => q.Category)
            .IsRequired()
            .HasMaxLength(Question.CategoryMaxLength)
            .UseCollation("NOCASE");

        builder.Property(q => q.Difficulty).IsRequired();

        builder.Ignore(q => q.BadPoints);

        builder.HasIndex(q => q.Category);
    }
}
