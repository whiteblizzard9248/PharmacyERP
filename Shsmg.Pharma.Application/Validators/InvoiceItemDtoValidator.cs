using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public sealed class InvoiceItemDtoValidator : AbstractValidator<InvoiceItemDto>
{
    public InvoiceItemDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Item description is required.")
            .MaximumLength(1000);

        RuleFor(x => x.Package.ToString())
            .MaximumLength(50);

        RuleFor(x => x.Mfg)
            .MaximumLength(100);

        RuleFor(x => x.Batch)
            .MaximumLength(50);

        RuleFor(x => x.ExpiryDate)
            .MaximumLength(20);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage("Rate must be greater than zero.");

        RuleFor(x => x.GstPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("GST percentage must be zero or greater.")
            .LessThanOrEqualTo(100).WithMessage("GST percentage must be 100 or less.");
    }
}
