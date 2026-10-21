using Blog.Models;
using Blog.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Data;

public static class RoleSeeder
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var name in new[] { RoleNames.Admin, RoleNames.Moderator, RoleNames.User })
        {
            if (await roles.RoleExistsAsync(name)) continue;
            var result = await roles.CreateAsync(new ApplicationRole { Name = name });
            if (!result.Succeeded)
                throw new InvalidOperationException($"Не удалось создать роль {name}: " +
                    string.Join(", ", result.Errors.Select(error => error.Description)));
        }
    }
}
