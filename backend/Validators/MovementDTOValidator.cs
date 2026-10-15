using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;

public class CreateEntryValidator : AbstractValidator<CreateEntryDTO>
{
    public CreateEntryValidator(AppDbContext db)
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