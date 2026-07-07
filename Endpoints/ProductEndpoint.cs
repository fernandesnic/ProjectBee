using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;
using System.ComponentModel.DataAnnotations;

public static class ProductEndpointsExtensions
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {

    var productsApi = app.MapGroup("/api/products");

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
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.Created($"/api/products/{product.Id}", product);
        });


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
                product.IsActive = true;
                product.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();

            return Results.Ok(new {mensagem = $"Produto atualizado com sucesso"});

        });

    }
}