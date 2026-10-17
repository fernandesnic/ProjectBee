using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;
using ProjectBee.DTO.Storage;

public abstract class BaseStorageValidator<T> : AbstractValidator<T> where T : IStorageDTO
{
    public BaseStorageValidator()
    {
        RuleFor(x => x.AddressStreet)
            .NotEmpty().WithMessage("A rua é obrigatória.");
        RuleFor(x => x.AddressCity)
            .NotEmpty().WithMessage("A cidade é obrigatória.");
        RuleFor(x => x.AddressNumber)
            .NotEmpty().WithMessage("O número do endereço é obrigatório.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(3).WithMessage("O nome deve ter pelo menos 3 caracteres.");
    }
}

public class CreateStorageDTOValidator : BaseStorageValidator<CreateStorageDTO>
{
    public CreateStorageDTOValidator(AppDbContext db)
    {
        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("O código do armazém é obrigatório.")
            .MinimumLength(3).WithMessage("O código do armazém deve ter pelo menos 3 caracteres.")
            .MaximumLength(20).WithMessage("O código do armazém deve ter no máximo 20 caracteres.")
            // IgnoreQueryFilters: o código de um armazém excluído continua reservado, para o histórico não ficar ambíguo.
            .MustAsync(async (idNumber, ct) =>
                !await db.Storages.IgnoreQueryFilters().AnyAsync(s => s.IdNumber == idNumber, ct))
            .WithMessage("Já existe um armazém com o código informado.");
    }

}

public class UpdateStorageDTOValidator : BaseStorageValidator<UpdateStorageDTO>
{
    public UpdateStorageDTOValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("O campo IsActive é obrigatório na atualização.");
    }
}
