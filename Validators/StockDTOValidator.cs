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
            .MustAsync(async (id, cancellationToken) =>
            {
                return await _db.Products.AnyAsync(p => p.Id == id, cancellationToken);
            }).WithMessage("O Produto informado não existe no banco de dados.");

        RuleFor(x => x.StorageId)
            .NotEmpty().WithMessage("O Id do armazem é obrigatório.") 
            .MustAsync(async (id, cancellationToken) =>
            {
                return await _db.Storages.AnyAsync(s => s.Id == id, cancellationToken);
            }).WithMessage("O Armazém informado não existe no banco de dados.");
        RuleFor(x => x.Batch)
            .NotEmpty().WithMessage("O lote é obrigatório.")
            .Matches("^[a-zA-Z0-9]+$").WithMessage("Apenas caracteres alfanuméricos são permitidos.");
        
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