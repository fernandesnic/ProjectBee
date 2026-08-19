using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;

public abstract class BaseStorageValidator<T> : AbstractValidator<T> where T : IStorageDTO
{
    public BaseStorageValidator()
    {
        RuleFor(x => x.IdNumber).NotEmpty().MinimumLength(3);
        RuleFor(x => x.AddressStreet).NotEmpty();
        RuleFor(x => x.AddressCity).NotEmpty();
        RuleFor(x => x.AddressNumber).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
    }
}

public class CreateStorageDTOValidator : BaseStorageValidator<CreateStorageDTO>
{

}

public class UpdateStorageDTOValidator : BaseStorageValidator<UpdateStorageDTO>
{
    
}