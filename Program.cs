using ProjectBee.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "API ProjectBee Online e operante!");

app.MapProductEndpoints();


app.Run();

public record CreateProductDTO(string Name, string SKU, string Desc, decimal Price);
public record UpdateProductDTO(string Name, string SKU, string Desc, decimal Price, bool IsActive);