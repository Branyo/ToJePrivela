using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Seed;

/// <summary>Anonymous objects are used because the entities keep their setters private.</summary>
public static class SeedData
{
    public static void ApplySeedData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().HasData(
            new { Id = 1, Name = "Admin", NameKey = "admin", Avatar = "🦉" },
            new { Id = 2, Name = "Brano", NameKey = "brano", Avatar = "🦊" },
            new { Id = 3, Name = "Duri", NameKey = "duri", Avatar = "🐼" });

        modelBuilder.Entity<QuestionCategory>().HasData(
            new { Id = 1, Name = "Cars", NameKey = "cars", AddedByPlayerId = (int?)1 },
            new { Id = 2, Name = "Sport", NameKey = "sport", AddedByPlayerId = (int?)1 },
            new { Id = 3, Name = "History", NameKey = "history", AddedByPlayerId = (int?)1 });
    }
}
