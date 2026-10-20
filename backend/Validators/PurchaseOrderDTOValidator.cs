using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Purchase;

// Aqui fica a forma da requisição. As regras que dependem do estado do pedido (status, pendente,
// preço do pedido) ficam no PurchaseService, porque precisam ser lidas dentro da transação.
public class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderDTO>
{
    public CreatePurchaseOrderValidator(AppDbContext db)
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("O fornecedor é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Suppliers.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O fornecedor informado não existe ou está inativo.");

        RuleFor(x => x.StorageId)
            .NotEmpty().WithMessage("O armazém de recebimento é obrigatório.")
            .MustAsync(async (id, ct) =>
                await db.Storages.AnyAsync(s => s.Id == id && s.IsActive, ct))
            .WithMessage("O armazém informado não existe ou está inativo.");

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
            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("O preço unitário deve ser maior que zero.")
                .PrecisionScale(14, 2, true).WithMessage("O preço unitário aceita no máximo 2 casas decimais.");
        });
    }
}

public class ReceivePurchaseOrderValidator : AbstractValidator<ReceivePurchaseOrderDTO>
{
    public ReceivePurchaseOrderValidator()
    {
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage("O número da nota fiscal é obrigatório.")
            .MaximumLength(20).WithMessage("O número da nota deve ter no máximo 20 caracteres.");

        RuleFor(x => x.PriceDivergenceReason)
            .MinimumLength(5).WithMessage("Descreva o motivo da divergência com pelo menos 5 caracteres.")
            .MaximumLength(250).WithMessage("O motivo da divergência deve ter no máximo 250 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.PriceDivergenceReason));

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("O campo RowVersion é obrigatório: envie a versão lida do pedido.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Informe pelo menos um item recebido.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => (i.PurchaseOrderItemId, i.Batch)).Distinct().Count() == items.Count)
            .WithMessage("O mesmo item e lote aparecem mais de uma vez. Some as quantidades na mesma linha.")
            .When(x => x.Items != null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.PurchaseOrderItemId)
                .NotEmpty().WithMessage("O item do pedido é obrigatório.");
            item.RuleFor(i => i.Batch).MustBeValidBatch();
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("A quantidade recebida deve ser maior que zero.");
            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("O preço da nota deve ser maior que zero.")
                .PrecisionScale(14, 2, true).WithMessage("O preço da nota aceita no máximo 2 casas decimais.");
        });
    }
}

public class CancelPurchaseOrderValidator : AbstractValidator<CancelPurchaseOrderDTO>
{
    public CancelPurchaseOrderValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo do cancelamento é obrigatório.")
            .MinimumLength(5).WithMessage("Descreva o motivo com pelo menos 5 caracteres.")
            .MaximumLength(250).WithMessage("O motivo deve ter no máximo 250 caracteres.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("O campo RowVersion é obrigatório: envie a versão lida do pedido.");
    }
}
