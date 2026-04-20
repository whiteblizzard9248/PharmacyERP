namespace Shsmg.Pharma.Domain.Models;

public class InventoryItem : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public string Package { get; set; } = string.Empty;
    public string Mfg { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;

    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public decimal Rate { get; set; }
    public decimal GstPercentage { get; set; }
}
