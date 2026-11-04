namespace StockSmart.Exceptions;

/// <summary>
/// Thrown when a sale breaks a business rule (no lines, quantity below 1,
/// or a product that isn't available). Nothing is saved (FR4).
/// </summary>
public class InvalidSaleException : Exception
{
    public InvalidSaleException(string message) : base(message)
    {
    }
}