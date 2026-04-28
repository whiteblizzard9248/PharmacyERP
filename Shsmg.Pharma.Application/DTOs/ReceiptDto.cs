namespace Shsmg.Pharma.Application.DTOs;

public class ReceiptSummaryDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class ReceiptDetailDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}

public class CreateReceiptDto
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public Guid CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}

public class UpdateReceiptDto : CreateReceiptDto
{
    public Guid Id { get; set; }
}
