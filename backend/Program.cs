using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;
using ProjectBee.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole apenas o token (sem o prefixo 'Bearer')."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

builder.Services.AddIdentityCore<AppUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDTOValidator>();

builder.Services.AddAuthorization();

builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Chave JWT ausente!")))
        };
    });

var app = builder.Build();

await app.SeedIdentityAsync();

app.UseExceptionHandler();
app.UseCors("FrontendDev");
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/", () => "API ProjectBee Online e operante!");


app.MapAuthEndpoints();

app.MapProductEndpoints();

app.MapStorageEndpoints();

app.MapStockEndpoints();

app.Run();

public record LoginDTO(string Email, string Password);
public record RegisterDTO(string Nome, string Email, string Password);

public record CreateProductDTO(string Name, string SKU, string Desc, decimal Price) : IProductDTO;
public record UpdateProductDTO(string Name, string Desc, decimal Price, bool IsActive) : IProductDTO;
public record ProductResponseDTO(Guid Id, string Name, string SKU, string Desc, decimal Price);


public record CreateStorageDTO(string IdNumber, string Name, string AddressNumber, string AddressStreet, string AddressCity) : IStorageDTO;
public record UpdateStorageDTO(string IdNumber, string Name, string AddressNumber, string AddressStreet, string AddressCity, bool IsActive) : IStorageDTO;
public record StorageResponseDTO(Guid Id, string Name, string IdNumber, string AddressNumber, string AddressStreet, string AddressCity);

public record CreateStockDTO(Guid ProductId, Guid StorageId, int Balance, string Batch) : IStockDTO;
public record UpdateStockDTO(int Balance);
public record StockResponseDTO(Guid ProductId, string ProductName, Guid StorageId, string StorageName, string StorageAddress, int Balance, string Batch);