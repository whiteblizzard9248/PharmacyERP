using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CompanyDto>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Company address is required.")
            .MaximumLength(2000);

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MaximumLength(50);

        RuleFor(x => x.ContactNumber)
            .NotEmpty().WithMessage("Contact number is required.")
            .MaximumLength(20);
    }
}
