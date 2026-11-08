namespace StockSmart.Models;

/// <summary>Sales figures for one date range (FR7). From is included, To is not.</summary>
public class SalesReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int SaleCount { get; set; }
    public decimal TotalSales { get; set; }
    public int ItemsSold { get; set; }
    public List<BestSeller> BestSellers { get; set; } = new();
    public List<DailyTotal> DailyTotals { get; set; } = new();
}

/// <summary>One product in the best-sellers list.</summary>
public class BestSeller
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>The sales for one day inside the range.</summary>
public class DailyTotal
{
    public DateTime Day { get; set; }
    public int SaleCount { get; set; }
    public decimal Total { get; set; }
}