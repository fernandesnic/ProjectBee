using ProjectBee.Data;
using ProjectBee.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "API ProjectBee Online e operante!");

var produtcsApi = app.MapGroup("/api/produtcs");

// Rota Post

produtcsApi.MapPost("/", async (Product produtc, AppDbContext db) =>
{
    db.Products.Add(produtc);
    await db.SaveChangesAsync();

    return Results.Created($"/api/produtcs/{produtc.Id}", produtc);

});

produtcsApi.MapGet("/", async (AppDbContext db) =>
{

    var produtcs = await db.Products.ToListAsync();
    return Results.Ok(produtcs);

});

app.Run();
