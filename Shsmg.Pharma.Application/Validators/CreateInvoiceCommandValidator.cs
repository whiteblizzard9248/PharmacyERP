using FluentValidation;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Validators;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.PatientName)
            .NotEmpty().WithMessage("Patient name is required.")
            .MaximumLength(150);

        RuleFor(x => x.DoctorName)
            .NotEmpty().WithMessage("Doctor name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Invoice must contain at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new InvoiceItemDtoValidator());
    }
}
