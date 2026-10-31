namespace StockSmart.Models;

/// <summary>A stock item (Product table). Linked to its supplier by SupplierId.</summary>
public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public int SupplierId { get; set; }

    /// <summary>True when stock has dropped to or below the reorder level (low-stock alert).</summary>
    public bool IsLowStock => Quantity <= ReorderLevel;
}