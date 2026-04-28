using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class PaymentService(
    IPharmacyDbContext context,
    IValidator<CreatePaymentDto> validator,
    ICurrentUserAccessor currentUserAccessor) : IPaymentService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly IValidator<CreatePaymentDto> _validator = validator;
    private readonly ICurrentUserAccessor _currentUserAccessor = currentUserAccessor;

    public async Task<IEnumerable<PaymentSummaryDto>> GetPaymentsAsync(string? search = null)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseInvoice)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(x =>
                EF.Functions.Like(x.PaymentNumber, pattern) ||
                EF.Functions.Like(x.Supplier!.Name, pattern) ||
                (x.PurchaseInvoice != null && EF.Functions.Like(x.PurchaseInvoice.PurchaseInvoiceNumber, pattern)) ||
                EF.Functions.Like(x.ReferenceNumber, pattern));
        }

        return await query
            .OrderByDescending(x => x.PaymentDate)
            .Select(x => new PaymentSummaryDto
            {
                Id = x.Id,
                PaymentNumber = x.PaymentNumber,
                Date = x.PaymentDate,
                SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                PurchaseInvoiceNumber = x.PurchaseInvoice != null ? x.PurchaseInvoice.PurchaseInvoiceNumber : string.Empty,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod
            })
            .ToListAsync();
    }

    public async Task<PaymentDetailDto?> GetPaymentByIdAsync(Guid id)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseInvoice)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new PaymentDetailDto
            {
                Id = x.Id,
                PaymentNumber = x.PaymentNumber,
                Date = x.PaymentDate,
                SupplierId = x.SupplierId,
                SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                PurchaseInvoiceId = x.PurchaseInvoiceId,
                PurchaseInvoiceNumber = x.PurchaseInvoice != null ? x.PurchaseInvoice.PurchaseInvoiceNumber : string.Empty,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                ReferenceNumber = x.ReferenceNumber,
                Notes = x.Notes,
                RowVersion = x.RowVersion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreatePaymentAsync(CreatePaymentDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == dto.SupplierId)
            ?? throw new ValidationException(new[] { new ValidationFailure("SupplierId", "Supplier not found.") });

        await ValidatePurchaseInvoiceAsync(dto.PurchaseInvoiceId, dto.SupplierId);
        var now = DateTime.UtcNow;
        var actor = _currentUserAccessor.GetCurrentUserIdentifier();

        var payment = new Payment
        {
            PaymentNumber = string.IsNullOrWhiteSpace(dto.PaymentNumber) ? GeneratePaymentNumber(dto.Date) : dto.PaymentNumber.Trim(),
            PaymentDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            SupplierId = dto.SupplierId,
            PurchaseInvoiceId = dto.PurchaseInvoiceId,
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

        ApplyPayment(supplier, dto.Amount);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment.Id;
    }

    public async Task<Guid> UpdatePaymentAsync(UpdatePaymentDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);

        var payment = await _context.Payments.FirstOrDefaultAsync(x => x.Id == dto.Id)
            ?? throw new Exception("Payment not found.");

        var existingSupplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == payment.SupplierId)
            ?? throw new Exception("Existing payment supplier not found.");
        ReversePayment(existingSupplier, payment.Amount);

        var newSupplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == dto.SupplierId)
            ?? throw new ValidationException(new[] { new ValidationFailure("SupplierId", "Supplier not found.") });
        await ValidatePurchaseInvoiceAsync(dto.PurchaseInvoiceId, dto.SupplierId);

        _context.Entry(payment).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

        payment.PaymentNumber = string.IsNullOrWhiteSpace(dto.PaymentNumber) ? payment.PaymentNumber : dto.PaymentNumber.Trim();
        payment.PaymentDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        payment.SupplierId = dto.SupplierId;
        payment.PurchaseInvoiceId = dto.PurchaseInvoiceId;
        payment.Amount = dto.Amount;
        payment.PaymentMethod = dto.PaymentMethod.Trim();
        payment.ReferenceNumber = dto.ReferenceNumber.Trim();
        payment.Notes = dto.Notes.Trim();
        payment.LastModified = DateTime.UtcNow;
        payment.LastModifiedBy = _currentUserAccessor.GetCurrentUserIdentifier();

        ApplyPayment(newSupplier, dto.Amount);
        await _context.SaveChangesAsync();
        return payment.Id;
    }

    public async Task DeletePaymentAsync(Guid id)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new Exception("Payment not found.");

        if (payment.IsDeleted)
            return;

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == payment.SupplierId)
            ?? throw new Exception("Payment supplier not found.");

        ReversePayment(supplier, payment.Amount);
        payment.IsDeleted = true;
        payment.LastModified = DateTime.UtcNow;
        payment.LastModifiedBy = _currentUserAccessor.GetCurrentUserIdentifier();
        await _context.SaveChangesAsync();
    }

    private async Task<PurchaseInvoice?> ValidatePurchaseInvoiceAsync(Guid? purchaseInvoiceId, Guid supplierId)
    {
        if (!purchaseInvoiceId.HasValue)
            return null;

        var invoice = await _context.PurchaseInvoices.FirstOrDefaultAsync(x => x.Id == purchaseInvoiceId.Value)
            ?? throw new ValidationException(new[] { new ValidationFailure("PurchaseInvoiceId", "Purchase invoice not found.") });

        if (invoice.SupplierId != supplierId)
            throw new ValidationException(new[] { new ValidationFailure("PurchaseInvoiceId", "Selected purchase invoice does not belong to the selected supplier.") });

        return invoice;
    }

    private static void ApplyPayment(Supplier supplier, decimal amount)
    {
        supplier.OutstandingAmount = Math.Max(0, supplier.OutstandingAmount - amount);
    }

    private static void ReversePayment(Supplier supplier, decimal amount)
    {
        supplier.OutstandingAmount += amount;
    }

    private static string GeneratePaymentNumber(DateTime date)
        => $"PAY-{date:yyyyMMdd}-{Guid.NewGuid():N}"[..18];
}
