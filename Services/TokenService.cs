using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) 
    {  
        _config = config;
    }

    public string GenerateToken(IdentityUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? throw new InvalidOperationException($"Usuário {user.Id} está sem email!"))
        };
        
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Chave JWT ausente!")));

        var signingCredentials =  new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var objectToken = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"], 
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: signingCredentials
            );

        var handler = new JwtSecurityTokenHandler();

        return handler.WriteToken(objectToken);
    }
}