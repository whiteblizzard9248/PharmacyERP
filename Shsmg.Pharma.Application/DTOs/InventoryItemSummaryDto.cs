namespace Shsmg.Pharma.Application.DTOs;

public class InventoryItemSummaryDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public decimal Rate { get; set; }
    public decimal GstPercentage { get; set; }
    public bool IsLowStock => QuantityInStock <= 0 || QuantityInStock <= ReorderLevel;
    public int ReorderLevel { get; set; }
}
