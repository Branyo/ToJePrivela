using Microsoft.EntityFrameworkCore;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Seed;

/// <summary>Anonymous objects are used because the entities keep their setters private.</summary>
public static class SeedData
{
    public static void ApplySeedData(this ModelBuilder modelBuilder)
    {
        // The reserved account (inserted by the AddAccounts migration) owns them until the first admin takes it over.
        modelBuilder.Entity<Player>().HasData(
            new { Id = 1, AccountId = Account.ReservedId, Name = "Admin", NameKey = "admin", Avatar = "🦉" },
            new { Id = 2, AccountId = Account.ReservedId, Name = "Brano", NameKey = "brano", Avatar = "🦊" },
            new { Id = 3, AccountId = Account.ReservedId, Name = "Duri", NameKey = "duri", Avatar = "🐼" });

        modelBuilder.Entity<QuestionCategory>().HasData(
            new { Id = 1, Name = "Cars", NameKey = "cars" },
            new { Id = 2, Name = "Sport", NameKey = "sport" },
            new { Id = 3, Name = "History", NameKey = "history" });
    }
}
