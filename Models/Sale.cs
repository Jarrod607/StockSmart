namespace StockSmart.Models;

/// <summary>One sale transaction (Sale table), recorded by a user.</summary>
public class Sale
{
    public int SaleId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal Total { get; set; }
    public int UserId { get; set; }

    /// <summary>The lines in this sale. Filled in by the repository when needed.</summary>
    public List<SaleItem> Items { get; set; } = new();
}