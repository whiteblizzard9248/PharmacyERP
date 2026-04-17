namespace Shsmg.Pharma.Domain.Models;

public class Company : BaseEntity // BaseEntity contains Id, IsDeleted, etc.
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string LicenseNumber { get; set; } = string.Empty; // e.g., DL No. KA SMG 20-21/865
    public string ContactNumber { get; set; } = string.Empty;
}