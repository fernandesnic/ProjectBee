using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;
public abstract class BaseProductValidator<T> : AbstractValidator<T> where T : IProductDTO
{
    public BaseProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .WithMessage("Nome deve ter pelo menos 3 caracteres."); 
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Preço deve ser maior que 0");
    }
}
public class CreateProductDTOValidator : BaseProductValidator<CreateProductDTO>
{
    private readonly AppDbContext _db;
    public CreateProductDTOValidator(AppDbContext db)
    {
        _db = db;

        RuleFor(x => x.SKU)
            .NotEmpty()
            .Matches(@"^[A-Z0-9-]{3,15}$")
            .WithMessage("O SKU deve conter apenas letras maiúsculas, números e traços, entre 3 a 15 caracteres.")
            .MustAsync(async (sku, cancellation) =>
            {
                bool exists = await _db.Products.AnyAsync(p => p.SKU == sku);
                return !exists;
            })
            .WithMessage("Já existe produto com SKU infomrado.");
    }
}

public class UpdateProductDTOValidator : BaseProductValidator<UpdateProductDTO>
{
    public UpdateProductDTOValidator()
    {
        RuleFor(x => x.IsActive).NotNull();
    }
}