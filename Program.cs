using ProjectBee.Data;
using ProjectBee.Models;
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

var productsApi = app.MapGroup("/api/products");

// Rota Post
productsApi.MapPost("/", async (CreateProductDTO dto, AppDbContext db) =>
{
    var product = new Product
    {
        Name = dto.Name,
        SKU = dto.SKU,
        Desc = dto.Desc,
        Price = dto.Price,
        Id = Guid.NewGuid(),
        IsActive = true,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/api/products/{product.Id}", product);
});

// Rota Get

productsApi.MapGet("/", async (AppDbContext db) =>
{
    var products = await db.Products.ToListAsync();
    return Results.Ok(products);

});

productsApi.MapDelete("/{id}", async (AppDbContext db, Guid id) =>
{

   var product = await db.Products.FindAsync(id);
   if (product == null)
   {
   return Results.NotFound(new {mensagem = $"Produto não encontrado"});
   }
    db.Products.Remove(product);

    await db.SaveChangesAsync();

    return Results.NoContent();

});

productsApi.MapPut("/{id}", async (UpdateProductDTO dto, AppDbContext db, Guid id) =>
{

   var product = await db.Products.FindAsync(id);
   if (product == null)
   {
   return Results.NotFound(new {mensagem = $"Produto não encontrado"});
   }
    
        product.Name = dto.Name;
        product.SKU = dto.SKU;
        product.Desc = dto.Desc;
        product.Price = dto.Price;
        product.IsActive = true;
        product.UpdatedAt = DateTime.Now;

    await db.SaveChangesAsync();

    return Results.Ok(new {mensagem = $"Produto atualizado com sucesso"});

});

app.Run();

public record CreateProductDTO(string Name, string SKU, string Desc, decimal Price);
public record UpdateProductDTO(string Name, string SKU, string Desc, decimal Price, bool IsActive);
