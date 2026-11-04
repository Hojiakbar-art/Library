using Application.Identity;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        await context.Database.MigrateAsync();

        if (!await roleManager.RoleExistsAsync(UserRole.Admin.ToString()))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(UserRole.Admin.ToString()));
        }

        if (!await roleManager.RoleExistsAsync(UserRole.Librarian.ToString()))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(UserRole.Librarian.ToString()));
        }

        if (!await roleManager.RoleExistsAsync(UserRole.Reader.ToString()))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(UserRole.Reader.ToString()));
        }

        if (await userManager.FindByEmailAsync("admin@mail.com") == null)
        {
            var admin = new User
            {
                UserName = "admin@mail.com",
                Email = "admin@mail.com",
                FirstName = "System",
                LastName = "Admin",
                BirthDate = new DateOnly(1990, 1, 1)
            };

            await userManager.CreateAsync(admin, "Admin123");
            await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
        }

        if (await userManager.FindByEmailAsync("librarian@mail.com") == null)
        {
            var librarian = new User
            {
                UserName = "librarian@mail.com",
                Email = "librarian@mail.com",
                FirstName = "System",
                LastName = "Librarian",
                BirthDate = new DateOnly(1985, 2, 2)
            };

            await userManager.CreateAsync(librarian, "Librarian123");
            await userManager.AddToRoleAsync(librarian, UserRole.Librarian.ToString());
        }
    }
}
