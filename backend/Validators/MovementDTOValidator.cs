using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;

public class StockMovementValidator : AbstractValidator<StockMovementDTO>
{
    public StockMovementValidator(AppDbContext db)
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("O Id do produto é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Products.AnyAsync(p => p.Id == id && p.IsActive, ct))
            .WithMessage("O produto informado não existe ou está inativo.");

        RuleFor(x => x.StorageId)
            .NotEmpty().WithMessage("O Id do armazém é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Storages.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O armazém informado não existe ou está inativo.");

        RuleFor(x => x.Batch)
            .NotEmpty().WithMessage("O lote é obrigatório.")
            .Matches("^[A-Z0-9-]{1,30}$")
            .WithMessage("O lote deve conter apenas letras maiúsculas, números e hífen (até 30 caracteres).");
    }
}

public class TransferValidator : AbstractValidator<TransferDTO>
{
    public TransferValidator(AppDbContext db)
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("O Id do produto é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Products.AnyAsync(p => p.Id == id && p.IsActive, ct))
            .WithMessage("O produto informado não existe ou está inativo.");

        RuleFor(x => x.FromStorageId)
            .NotEmpty().WithMessage("O armazém de origem é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Storages.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O armazém de origem não existe ou está inativo.");

        RuleFor(x => x.ToStorageId)
            .NotEmpty().WithMessage("O armazém de destino é obrigatório.")
            .NotEqual(x => x.FromStorageId).WithMessage("O armazém de origem e o de destino devem ser diferentes.")
            .MustAsync(async (id, ct) =>
                await db.Storages.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O armazém de destino não existe ou está inativo.");

        RuleFor(x => x.Batch)
            .NotEmpty().WithMessage("O lote é obrigatório.")
            .Matches("^[A-Z0-9-]{1,30}$")
            .WithMessage("O lote deve conter apenas letras maiúsculas, números e hífen (até 30 caracteres).");
    }
}

public class AdjustmentValidator : AbstractValidator<AdjustmentDTO>
{
    public AdjustmentValidator(AppDbContext db)
    {
        RuleFor(x => x.CountedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade contada não pode ser negativa.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo do ajuste é obrigatório.")
            .MinimumLength(5).WithMessage("Descreva o motivo com pelo menos 5 caracteres.")
            .MaximumLength(250).WithMessage("O motivo deve ter no máximo 250 caracteres.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("O Id do produto é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Products.AnyAsync(p => p.Id == id && p.IsActive, ct))
            .WithMessage("O produto informado não existe ou está inativo.");

        RuleFor(x => x.StorageId)
            .NotEmpty().WithMessage("O Id do armazém é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Storages.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O armazém informado não existe ou está inativo.");

        RuleFor(x => x.Batch)
            .NotEmpty().WithMessage("O lote é obrigatório.")
            .Matches("^[A-Z0-9-]{1,30}$")
            .WithMessage("O lote deve conter apenas letras maiúsculas, números e hífen (até 30 caracteres).");
    }
}