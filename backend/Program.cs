using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Filters;
using ProjectBee.Interfaces;
using ProjectBee.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using ProjectBee.Services;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("Cors:AllowedOrigins não configurado.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
        options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProjectBee API",
        Version = "v1",
        Description =
            "API de controle de estoque multi-armazém com rastreabilidade por lote.\n\n" +
            "O saldo é modelado como entidade própria, com chave composta por " +
            "**produto + armazém + lote** — o mesmo item pode existir em quantidades " +
            "diferentes em locais diferentes.\n\n" +
            "**Autenticação:** faça login em `/api/auth/login` com o usuário demo, " +
            "copie o token e cole no botão *Authorize* acima.",
        Contact = new OpenApiContact
        {
            Name = "Nicolas Fernandes",
            Url = new Uri("https://github.com/fernandesnic/ProjectBee")
        }
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole apenas o token (sem o prefixo 'Bearer')."
    });

    options.OperationFilter<AuthorizeOperationFilter>();
});

builder.Services.AddIdentityCore<AppUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// Sem isso, corpo inválido (JSON malformado, "price": "abc") vira 400 sem corpo fora de Development.
// Lançando a exceção, o GlobalExceptionHandler responde com ProblemDetails, como os outros erros.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDTOValidator>();
// Mensagens padrão do FluentValidation em português, para qualquer regra sem WithMessage.
ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("pt-BR");

builder.Services.AddAuthorization();

builder.Services.AddScoped<StockMovementService>();

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
await app.SeedBusinessDataAsync();

app.UseExceptionHandler();
app.UseCors("FrontendDev");
app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health");

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/", () => "API ProjectBee Online e operante!");


app.MapAuthEndpoints();

app.MapProductEndpoints();

app.MapStorageEndpoints();

app.MapStockEndpoints();

app.MapMovementEndpoints();

app.Run();
