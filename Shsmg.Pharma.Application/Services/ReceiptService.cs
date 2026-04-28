using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class ReceiptService(
    IPharmacyDbContext context,
    IValidator<CreateReceiptDto> validator,
    ICurrentUserAccessor currentUserAccessor) : IReceiptService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly IValidator<CreateReceiptDto> _validator = validator;
    private readonly ICurrentUserAccessor _currentUserAccessor = currentUserAccessor;

    public async Task<IEnumerable<ReceiptSummaryDto>> GetReceiptsAsync(string? search = null)
    {
        var query = _context.Receipts
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(x =>
                EF.Functions.Like(x.ReceiptNumber, pattern) ||
                EF.Functions.Like(x.Customer!.Name, pattern) ||
                (x.Invoice != null && EF.Functions.Like(x.Invoice.InvoiceNumber, pattern)) ||
                EF.Functions.Like(x.ReferenceNumber, pattern));
        }

        return await query
            .OrderByDescending(x => x.ReceiptDate)
            .Select(x => new ReceiptSummaryDto
            {
                Id = x.Id,
                ReceiptNumber = x.ReceiptNumber,
                Date = x.ReceiptDate,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                InvoiceNumber = x.Invoice != null ? x.Invoice.InvoiceNumber : string.Empty,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod
            })
            .ToListAsync();
    }

    public async Task<ReceiptDetailDto?> GetReceiptByIdAsync(Guid id)
    {
        return await _context.Receipts
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ReceiptDetailDto
            {
                Id = x.Id,
                ReceiptNumber = x.ReceiptNumber,
                Date = x.ReceiptDate,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                InvoiceId = x.InvoiceId,
                InvoiceNumber = x.Invoice != null ? x.Invoice.InvoiceNumber : string.Empty,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                ReferenceNumber = x.ReferenceNumber,
                Notes = x.Notes,
                RowVersion = x.RowVersion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateReceiptAsync(CreateReceiptDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == dto.CustomerId)
            ?? throw new ValidationException(new[] { new ValidationFailure("CustomerId", "Customer not found.") });

        var invoice = await ValidateInvoiceAsync(dto.InvoiceId, dto.CustomerId);
        var now = DateTime.UtcNow;
        var actor = _currentUserAccessor.GetCurrentUserIdentifier();

        var receipt = new Receipt
        {
            ReceiptNumber = string.IsNullOrWhiteSpace(dto.ReceiptNumber) ? GenerateReceiptNumber(dto.Date) : dto.ReceiptNumber.Trim(),
            ReceiptDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            CustomerId = dto.CustomerId,
            InvoiceId = dto.InvoiceId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod.Trim(),
            ReferenceNumber = dto.ReferenceNumber.Trim(),
            Notes = dto.Notes.Trim(),
            CreatedAt = now,
            CreatedBy = actor,
            LastModified = now,
            LastModifiedBy = actor,
            RowVersion = dto.RowVersion
        };

        ApplyReceipt(customer, dto.Amount);
        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();
        return receipt.Id;
    }

    public async Task<Guid> UpdateReceiptAsync(UpdateReceiptDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);

        var receipt = await _context.Receipts.FirstOrDefaultAsync(x => x.Id == dto.Id)
            ?? throw new Exception("Receipt not found.");

        var existingCustomer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == receipt.CustomerId)
            ?? throw new Exception("Existing receipt customer not found.");
        ReverseReceipt(existingCustomer, receipt.Amount);

        var newCustomer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == dto.CustomerId)
            ?? throw new ValidationException(new[] { new ValidationFailure("CustomerId", "Customer not found.") });
        await ValidateInvoiceAsync(dto.InvoiceId, dto.CustomerId);

        _context.Entry(receipt).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

        receipt.ReceiptNumber = string.IsNullOrWhiteSpace(dto.ReceiptNumber) ? receipt.ReceiptNumber : dto.ReceiptNumber.Trim();
        receipt.ReceiptDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        receipt.CustomerId = dto.CustomerId;
        receipt.InvoiceId = dto.InvoiceId;
        receipt.Amount = dto.Amount;
        receipt.PaymentMethod = dto.PaymentMethod.Trim();
        receipt.ReferenceNumber = dto.ReferenceNumber.Trim();
        receipt.Notes = dto.Notes.Trim();
        receipt.LastModified = DateTime.UtcNow;
        receipt.LastModifiedBy = _currentUserAccessor.GetCurrentUserIdentifier();

        ApplyReceipt(newCustomer, dto.Amount);
        await _context.SaveChangesAsync();
        return receipt.Id;
    }

    public async Task DeleteReceiptAsync(Guid id)
    {
        var receipt = await _context.Receipts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new Exception("Receipt not found.");

        if (receipt.IsDeleted)
            return;

        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == receipt.CustomerId)
            ?? throw new Exception("Receipt customer not found.");

        ReverseReceipt(customer, receipt.Amount);
        receipt.IsDeleted = true;
        receipt.LastModified = DateTime.UtcNow;
        receipt.LastModifiedBy = _currentUserAccessor.GetCurrentUserIdentifier();
        await _context.SaveChangesAsync();
    }

    private async Task<Invoice?> ValidateInvoiceAsync(Guid? invoiceId, Guid customerId)
    {
        if (!invoiceId.HasValue)
            return null;

        var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId.Value)
            ?? throw new ValidationException(new[] { new ValidationFailure("InvoiceId", "Invoice not found.") });

        if (invoice.CustomerId != customerId)
            throw new ValidationException(new[] { new ValidationFailure("InvoiceId", "Selected invoice does not belong to the selected customer.") });

        return invoice;
    }

    private static void ApplyReceipt(Customer customer, decimal amount)
    {
        customer.OutstandingAmount = Math.Max(0, customer.OutstandingAmount - amount);
    }

    private static void ReverseReceipt(Customer customer, decimal amount)
    {
        customer.OutstandingAmount += amount;
    }

    private static string GenerateReceiptNumber(DateTime date)
        => $"RCPT-{date:yyyyMMdd}-{Guid.NewGuid():N}"[..19];
}
