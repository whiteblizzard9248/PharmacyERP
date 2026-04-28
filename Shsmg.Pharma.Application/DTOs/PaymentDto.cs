namespace Shsmg.Pharma.Application.DTOs;

public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string PurchaseInvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class PaymentDetailDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid? PurchaseInvoiceId { get; set; }
    public string PurchaseInvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}

public class CreatePaymentDto
{
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public Guid SupplierId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}

public class UpdatePaymentDto : CreatePaymentDto
{
    public Guid Id { get; set; }
}
