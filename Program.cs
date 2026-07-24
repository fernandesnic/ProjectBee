using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDTOValidator>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "API ProjectBee Online e operante!");

app.MapProductEndpoints();

app.MapStorageEndpoints();

app.MapStockEndpoints();

app.Run();

public record CreateProductDTO(string Name, string SKU, string Desc, decimal Price) : IProductDTO;
public record UpdateProductDTO(string Name, string SKU, string Desc, decimal Price, bool IsActive) : IProductDTO;
public record ProductResponseDTO(Guid Id, string Name, string SKU, string Desc, decimal Price);


public record CreateStorageDTO(string IdNumber, string AddressNumber, string AddressStreet, string AddressCity) : IStorageDTO;
public record UpdateStorageDTO(string IdNumber, string AddressNumber, string AddressStreet, string AddressCity) : IStorageDTO;
public record StorageResponseDTO(Guid Id, string IdNumber, string AddressNumber, string AddressStreet, string AddressCity);

public record CreateStockDTO(Guid ProductId, Guid StorageId, int Balance, string Batch) : IStockDTO;
public record UpdateStockDTO(int Balance);
public record StockResponseDTO(Guid ProductId, string ProductName, Guid StorageId, string StorageName, int Balance, string Batch);