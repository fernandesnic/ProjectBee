using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Interfaces;

public abstract class BaseStorageValidator<T> : AbstractValidator<T> where T : IStorageDTO
{
    public BaseStorageValidator()
    {
        RuleFor(x => x.IdNumber).NotEmpty().Length(3);
        RuleFor(x => x.AddressStreet).NotEmpty();
        RuleFor(x => x.AddressCity).NotEmpty();
        RuleFor(x => x.AddressNumber).NotEmpty();
    }
}

public class CreateStorageDTOValidator : BaseStorageValidator<CreateStorageDTO>
{

}

public class UpdateStorageDTOValidator : BaseStorageValidator<UpdateStorageDTO>
{
    
}