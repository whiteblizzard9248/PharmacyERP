namespace Shsmg.Pharma.Domain.Models;

public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}