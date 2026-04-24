using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public sealed class PurchaseInvoiceItemDtoValidator : AbstractValidator<PurchaseInvoiceItemDto>
{
    public PurchaseInvoiceItemDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().MaximumLength(1000);

        RuleFor(x => x.HsnCode)
            .MaximumLength(10);

        RuleFor(x => x.Package)
            .MaximumLength(50);

        RuleFor(x => x.Mfg)
            .MaximumLength(100);

        RuleFor(x => x.Batch)
            .MaximumLength(50);

        RuleFor(x => x.ExpiryDate)
            .MaximumLength(20);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Rate)
            .GreaterThan(0);

        RuleFor(x => x.GstPercentage)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100);
    }
}
