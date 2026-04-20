using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public sealed class InventoryItemDtoValidator : AbstractValidator<InventoryItemDto>
{
    public InventoryItemDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Item description is required.")
            .MaximumLength(1000);

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

        RuleFor(x => x.QuantityInStock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be zero or greater.");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder level must be zero or greater.");

        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage("Rate must be greater than zero.");

        RuleFor(x => x.GstPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("GST percentage must be zero or greater.")
            .LessThanOrEqualTo(100).WithMessage("GST percentage must be 100 or less.");
    }
}
