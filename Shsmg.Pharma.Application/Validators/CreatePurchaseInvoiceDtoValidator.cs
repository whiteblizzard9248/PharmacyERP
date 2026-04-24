using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public sealed class CreatePurchaseInvoiceDtoValidator : AbstractValidator<CreatePurchaseInvoiceDto>
{
    public CreatePurchaseInvoiceDtoValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Supplier is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Purchase invoice must contain at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new PurchaseInvoiceItemDtoValidator());
    }
}
