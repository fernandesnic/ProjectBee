using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;

public abstract class BaseStockValidator<T> : AbstractValidator<T> where T : IStockDTO
{
    protected readonly AppDbContext _db;

    public BaseStockValidator(AppDbContext db)
    {
        _db = db;

        RuleFor(x => x.Balance)
            .GreaterThan(0).WithMessage("O saldo inicial deve ser maior que zero.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("O Id do produto é obrigatório.")
            .MustAsync(async (id, ct) =>
                await _db.Products.AnyAsync(p => p.Id == id && p.IsActive, ct))
            .WithMessage("O produto informado não existe ou está inativo.");

        RuleFor(x => x.StorageId)
            .NotEmpty().WithMessage("O Id do armazém é obrigatório.")
            .MustAsync(async (id, ct) =>
                await _db.Storages.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O armazém informado não existe ou está inativo.");

        RuleFor(x => x.Batch)
            .NotEmpty().WithMessage("O lote é obrigatório.")
            .Matches("^[A-Z0-9-]{1,30}$")
            .WithMessage("O lote deve conter apenas letras maiúsculas, números e hífen (até 30 caracteres).");
    }
}

public class CreateStockValidator : BaseStockValidator<CreateStockDTO>
{
    public CreateStockValidator(AppDbContext db) : base(db)
    {
    }
}

public class UpdateStockValidator : AbstractValidator<UpdateStockDTO>
{
    public UpdateStockValidator()
    {
        RuleFor(x => x.Balance)
            .GreaterThan(0).WithMessage("O saldo deve ser maior que zero.");
    }
}