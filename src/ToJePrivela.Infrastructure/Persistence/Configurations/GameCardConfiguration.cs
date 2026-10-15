using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Configurations;

public sealed class GameCardConfiguration : IEntityTypeConfiguration<GameCard>
{
    public void Configure(EntityTypeBuilder<GameCard> builder)
    {
        builder.HasKey(card => card.Id);
        builder.Ignore(card => card.IsFalseAlarm);

        builder.HasOne<Game>()
            .WithMany(g => g.Cards)
            .HasForeignKey(card => card.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seats, not players: a deleted player's seat stays (as an unknown player), and so do its cards. Seats only go
        // with their game, which takes its cards along.
        builder.HasOne(card => card.Recipient)
            .WithMany()
            .HasForeignKey(card => card.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(card => card.Caller)
            .WithMany()
            .HasForeignKey(card => card.CallerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting a question (or its category, which takes its questions along) keeps the card, only without them.
        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(card => card.QuestionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<QuestionCategory>()
            .WithMany()
            .HasForeignKey(card => card.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
