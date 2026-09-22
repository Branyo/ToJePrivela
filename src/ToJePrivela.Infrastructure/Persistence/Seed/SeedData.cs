using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Seed;

/// <summary>Anonymous objects are used because the entities keep their setters private.</summary>
public static class SeedData
{
    public static void ApplySeedData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().HasData(
            new { Id = 1, Name = "Admin" },
            new { Id = 2, Name = "Brano" },
            new { Id = 3, Name = "Duri" });

        modelBuilder.Entity<QuestionCategory>().HasData(
            new { Id = 1, Name = "Cars", AddedByPlayerId = (int?)1 },
            new { Id = 2, Name = "Sport", AddedByPlayerId = (int?)1 },
            new { Id = 3, Name = "History", AddedByPlayerId = (int?)1 });
    }
}
