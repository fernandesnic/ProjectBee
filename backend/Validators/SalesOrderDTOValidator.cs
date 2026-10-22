using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Sales;
using ProjectBee.Models;

// Aqui fica a forma da requisição. O que depende do estado do estoque e do pedido (saldo
// disponível, status) fica no SalesService, porque precisa ser lido dentro da transação.
public class CreateSalesOrderValidator : AbstractValidator<CreateSalesOrderDTO>
{
    public CreateSalesOrderValidator(AppDbContext db)
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("O cliente é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Customers.AnyAsync(c => c.Id == id && c.IsActive, ct))
            .WithMessage("O cliente informado não existe ou está inativo.");

        RuleFor(x => x.StorageId)
            .NotEmpty().WithMessage("O armazém de expedição é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Storages.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O armazém informado não existe ou está inativo.");

        RuleFor(x => x.PaymentMethod)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A condição de pagamento é obrigatória.")
            .IsEnumName(typeof(PaymentMethod), caseSensitive: false)
            .WithMessage("Condição de pagamento inválida. Use Boleto, Pix ou CreditCard.")
            // Pix e cartão já existem no contrato para o front não mudar quando chegarem.
            .Must(method => string.Equals(method, nameof(PaymentMethod.Boleto), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Por enquanto só o boleto está disponível. Pix e cartão estão planejados.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("A observação deve ter no máximo 500 caracteres.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("O pedido precisa de pelo menos um item.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Cada produto pode aparecer uma vez no pedido. Some as quantidades na mesma linha.")
            .MustAsync(async (items, ct) =>
            {
                var ids = items.Select(i => i.ProductId).Distinct().ToList();
                return await db.Products.CountAsync(p => ids.Contains(p.Id) && p.IsActive, ct) == ids.Count;
            })
            .WithMessage("Um ou mais produtos do pedido não existem ou estão inativos.")
            .When(x => x.Items != null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("O produto é obrigatório.");
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
        });
    }
}

public class PayBoletoValidator : AbstractValidator<PayBoletoDTO>
{
    public PayBoletoValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("O campo RowVersion é obrigatório: envie a versão lida do pedido.");
    }
}
