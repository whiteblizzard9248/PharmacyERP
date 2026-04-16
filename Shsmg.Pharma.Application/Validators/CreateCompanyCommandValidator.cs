using FluentValidation;
using Shsmg.Pharma.Application.Features.Company.Commands;

namespace Shsmg.Pharma.Application.Validators;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyDto.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        RuleFor(x => x.CompanyDto.Address)
            .NotEmpty().WithMessage("Company address is required.")
            .MaximumLength(2000);

        RuleFor(x => x.CompanyDto.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MaximumLength(50);

        RuleFor(x => x.CompanyDto.ContactNumber)
            .NotEmpty().WithMessage("Contact number is required.")
            .MaximumLength(20);
    }
}
