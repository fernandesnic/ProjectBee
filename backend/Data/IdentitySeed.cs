using Microsoft.AspNetCore.Identity;
using ProjectBee.Models;

namespace ProjectBee.Data;

public static class IdentitySeedExtensions
{
    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        foreach (var role in new[] { Roles.Manager, Roles.Operator })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string demoEmail = "demo@projectbee.dev";
        var demoUser = await userManager.FindByEmailAsync(demoEmail);

        if (demoUser is null)
        {
            var demoPassword = configuration["Demo:Password"]
                ?? throw new InvalidOperationException("Senha do usuário demo não configurada.");

            demoUser = new AppUser
            {
                UserName = demoEmail,
                Email = demoEmail,
                Nome = "Usuário Demo",
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(demoUser, demoPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Falha ao criar usuário demo: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(demoUser, Roles.Manager))
        {
            await userManager.AddToRoleAsync(demoUser, Roles.Manager);
        }
    }
}
