using Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorklyDbContext>();

        var companyId = Guid.Parse("447A142D-A99A-4F41-9B08-36AD2EC30B2B");
        var engineeringAreaId = Guid.Parse("5F7F39B8-3B6D-4F7B-AD43-4C651CF62B01");
        var operationsAreaId = Guid.Parse("FE7150D1-D7F1-4792-A54B-783E14DBA938");

        var adminUserId = Guid.Parse("DEEB31E8-B04D-4E9B-9E76-10E563BC6337");
        var memberUserId = Guid.Parse("496E8B4D-9328-451A-BDC6-CB03C33821CF");
        var operationsUserId = Guid.Parse("F8465060-B637-4932-BC9C-4C4C31E91B8E");

        var users = new[]
        {
            new User
            {
                Id = adminUserId,
                Email = "admin@saas.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
                Role = UserRoles.Admin,
            },
            new User
            {
                Id = memberUserId,
                Email = "usuario@saas.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
                Role = UserRoles.Member,
                CompanyId = companyId,
                AreaId = engineeringAreaId
            },
            new User
            {
                Id = operationsUserId,
                Email = "operaciones@saas.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
                Role = UserRoles.Member,
                CompanyId = companyId,
                AreaId = operationsAreaId
            }
        };

        foreach (var user in users)
        {
            if (!db.Users.Any(existing => existing.Id == user.Id || existing.Email == user.Email))
            {
                db.Users.Add(user);
            }
        }

        await db.SaveChangesAsync();

        var preferences = new[]
        {
            new UserNotificationPreference
            {
                Id = Guid.Parse("CCE7F8AA-0E3C-4B7F-8D9F-11E6B2C73D4A"),
                UserId = adminUserId,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true,
                CreatedAt = DateTime.UtcNow
            },
            new UserNotificationPreference
            {
                Id = Guid.Parse("D9E4F2BB-1F4D-4C8A-9E0A-22F7C3D84E5B"),
                UserId = memberUserId,
                EmailNotificationsEnabled = false,
                InAppNotificationsEnabled = true,
                CreatedAt = DateTime.UtcNow
            },
            new UserNotificationPreference
            {
                Id = Guid.Parse("EAF5A3CC-2A5E-5D9A-AF1A-33A8D4E95F6C"),
                UserId = operationsUserId,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var preference in preferences)
        {
            var existing = db.UserNotificationPreferences
                .FirstOrDefault(item => item.UserId == preference.UserId);

            if (existing is null)
            {
                db.UserNotificationPreferences.Add(preference);
            }
            else
            {
                existing.EmailNotificationsEnabled = preference.EmailNotificationsEnabled;
                existing.InAppNotificationsEnabled = preference.InAppNotificationsEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }
}
