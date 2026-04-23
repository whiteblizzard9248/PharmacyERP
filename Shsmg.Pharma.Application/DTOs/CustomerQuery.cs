namespace Shsmg.Pharma.Application.DTOs;

public class CustomerQuery
{
    public string? Search { get; set; }

    public int? Type { get; set; }
    public bool? IsBlacklisted { get; set; }

    public decimal? MinOutstanding { get; set; }
    public decimal? MaxOutstanding { get; set; }

    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}