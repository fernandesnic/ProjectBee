using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;
using ProjectBee.DTO.Product;

public abstract class BaseProductValidator<T> : AbstractValidator<T> where T : IProductDTO
{
    public BaseProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(3).WithMessage("O nome deve ter pelo menos 3 caracteres.");
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("O preço deve ser maior que zero.");
    }
}
public class CreateProductDTOValidator : BaseProductValidator<CreateProductDTO>
{
    private readonly AppDbContext _db;
    public CreateProductDTOValidator(AppDbContext db)
    {
        _db = db;

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("O SKU é obrigatório.")
            .Matches(@"^[A-Z0-9-]{3,15}$")
            .WithMessage("O SKU deve conter apenas letras maiúsculas, números e traços, entre 3 e 15 caracteres.")
            // IgnoreQueryFilters: o SKU de um produto excluído continua reservado. Sem isso, a validação
            // passaria e o índice único do banco recusaria o INSERT com erro 500.
            .MustAsync(async (sku, ct) =>
                !await _db.Products.IgnoreQueryFilters().AnyAsync(p => p.SKU == sku, ct))
            .WithMessage("Já existe um produto com o SKU informado, ativo ou excluído.");
    }
}

public class UpdateProductDTOValidator : BaseProductValidator<UpdateProductDTO>
{
    public UpdateProductDTOValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("O campo IsActive é obrigatório na atualização.");
    }
}
