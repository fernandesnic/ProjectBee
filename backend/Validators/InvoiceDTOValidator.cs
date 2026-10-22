using FluentValidation;
using ProjectBee.DTO.Invoice;

// Status do pedido e saldo do estoque ficam no SalesService: precisam ser lidos dentro da transação.
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDTO>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.SalesOrderId)
            .NotEmpty().WithMessage("O pedido de venda é obrigatório.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("O campo RowVersion é obrigatório: envie a versão lida do pedido.");
    }
}
