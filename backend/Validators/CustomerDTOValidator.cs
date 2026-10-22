using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;
using ProjectBee.DTO.Customer;

public abstract class BaseCustomerValidator<T> : AbstractValidator<T> where T : ICustomerDTO
{
    public BaseCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(3).WithMessage("O nome deve ter pelo menos 3 caracteres.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");
        // E-mail e telefone são opcionais, mas quando vierem precisam fazer sentido.
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("O e-mail informado não é válido.")
            .MaximumLength(256).WithMessage("O e-mail deve ter no máximo 256 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone)
            .Matches(@"^[0-9()+\- ]{8,20}$").WithMessage("O telefone deve ter de 8 a 20 caracteres entre dígitos, espaço, parênteses, + e hífen.")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}

public class CreateCustomerDTOValidator : BaseCustomerValidator<CreateCustomerDTO>
{
    public CreateCustomerDTOValidator(AppDbContext db)
    {
        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("O CNPJ ou CPF é obrigatório.")
            .Matches(@"^(\d{11}|\d{14})$").WithMessage("Informe só os dígitos do CPF (11) ou do CNPJ (14).")
            // IgnoreQueryFilters: o documento de um cliente excluído continua reservado, como o do fornecedor.
            .MustAsync(async (document, ct) =>
                !await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Document == document, ct))
            .WithMessage("Já existe um cliente com o documento informado, ativo ou excluído.");
    }
}

public class UpdateCustomerDTOValidator : BaseCustomerValidator<UpdateCustomerDTO>
{
    public UpdateCustomerDTOValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("O campo IsActive é obrigatório na atualização.");
        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("O campo RowVersion é obrigatório na atualização: envie a versão lida do cliente.");
    }
}
