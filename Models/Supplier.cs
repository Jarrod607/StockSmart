namespace StockSmart.Models;

/// <summary>A supplier of products (Supplier table).</summary>
public class Supplier
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}