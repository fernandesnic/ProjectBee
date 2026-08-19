using Microsoft.AspNetCore.Identity;
using ProjectBee.Models;

public static class AuthEndpointsExtensions
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authApi = app.MapGroup("/api/auth");

        authApi.MapPost("/login", async (LoginDTO dto, UserManager<AppUser> userManager, TokenService tokenService) =>
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            var senhaValida = await userManager.CheckPasswordAsync(user, dto.Password);

            if (!senhaValida)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);

            var token = tokenService.GenerateToken(user, roles);

            return Results.Ok(new { token });
        });

        authApi.MapPost("/register", async (RegisterDTO dto, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager) =>
        {
            var usuarioExistente = await userManager.FindByEmailAsync(dto.Email);

            if (usuarioExistente != null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Email"] = ["Email já cadastrado"]
                });
            }

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Nome = dto.Nome
            };

            var result = await userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var erros = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                return Results.ValidationProblem(erros);
            }

            if (!await roleManager.RoleExistsAsync(Roles.Operator))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Operator));
            }

            await userManager.AddToRoleAsync(user, Roles.Operator);

            return Results.Ok(new { user.Id, user.Nome, user.Email });
        });
    }
}
