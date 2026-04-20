namespace Shsmg.Pharma.Application.DTOs;

public class InvoiceAuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
}
