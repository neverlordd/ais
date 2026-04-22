using AIS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIS.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        var adminExists = await dbContext.Users.AnyAsync(user => user.Username == "admin");
        if (adminExists)
        {
            return;
        }

        var admin = new User
        {
            Username = "admin",
            FullName = "Системный администратор",
            Role = UserRole.Admin
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, "admin");

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
    }
}
