using FluentValidation;
using Shsmg.Pharma.Application.Features.Invoices.Commands;

namespace Shsmg.Pharma.Application.Validators;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceDto.PatientName)
            .NotEmpty().WithMessage("Patient name is required.")
            .MaximumLength(150);

        RuleFor(x => x.InvoiceDto.DoctorName)
            .NotEmpty().WithMessage("Doctor name is required.")
            .MaximumLength(150);

        RuleFor(x => x.InvoiceDto.Items)
            .NotEmpty().WithMessage("Invoice must contain at least one item.");

        RuleForEach(x => x.InvoiceDto.Items)
            .SetValidator(new InvoiceItemDtoValidator());
    }
}
