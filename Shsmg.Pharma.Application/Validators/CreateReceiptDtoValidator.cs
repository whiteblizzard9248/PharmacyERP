using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public class CreateReceiptDtoValidator : AbstractValidator<CreateReceiptDto>
{
    public CreateReceiptDtoValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
