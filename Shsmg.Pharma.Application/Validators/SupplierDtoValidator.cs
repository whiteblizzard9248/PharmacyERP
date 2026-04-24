using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public sealed class SupplierDtoValidator : AbstractValidator<SupplierDto>
{
    public SupplierDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.ContactPerson)
            .MaximumLength(150);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20);

        RuleFor(x => x.Email)
            .MaximumLength(255);

        RuleFor(x => x.GstNumber)
            .MaximumLength(30);
    }
}
