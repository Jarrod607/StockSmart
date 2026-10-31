namespace StockSmart.Models;

/// <summary>One line of a sale (SaleItem table). UnitPrice is copied at the time of sale,
/// so later price changes don't alter past sales.</summary>
public class SaleItem
{
    public int SaleItemId { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}