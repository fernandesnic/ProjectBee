using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

public static class ProductEndpointsExtensions
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {

    var productsApi = app.MapGroup("/api/products")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager));

        productsApi.MapPost("/", async (CreateProductDTO dto, IValidator < CreateProductDTO > validator, AppDbContext db) =>
        {

            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            } 

            var product = new Product
            {
                Name = dto.Name,
                SKU = dto.SKU,
                Desc = dto.Desc,
                Price = dto.Price,
                Id = Guid.NewGuid(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var responseDTO = new ProductResponseDTO(product.Id, product.Name, product.SKU, product.Desc, product.Price);

            return Results.Created($"/api/products/{product.Id}", responseDTO);
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager));


        productsApi.MapGet("/", async (AppDbContext db) =>
        {
            var responder = await db.Products
                .AsNoTracking()
                .Select(p => new ProductResponseDTO(
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.Desc,
                    p.Price
                    ))
                .ToListAsync();

            return Results.Ok(responder);

        });

        productsApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var responder = await db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductResponseDTO(
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.Desc,
                    p.Price
                    ))
                .FirstOrDefaultAsync();

            if (responder == null)
            {
                return Results.NotFound(new { mensagem = $"Produto não encontrado" });
            }

            return Results.Ok(responder);

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

        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager));

        productsApi.MapPut("/{id}", async (UpdateProductDTO dto, IValidator < UpdateProductDTO > validator, AppDbContext db, Guid id) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var product = await db.Products.FindAsync(id);
        if (product == null)
        {
        return Results.NotFound(new {mensagem = $"Produto não encontrado"});
        }
            
                product.Name = dto.Name;
                product.Desc = dto.Desc;
                product.Price = dto.Price;
                product.IsActive = dto.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new {mensagem = $"Produto atualizado com sucesso"});

        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager));

    }
}