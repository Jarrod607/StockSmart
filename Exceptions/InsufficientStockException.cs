namespace StockSmart.Exceptions;

/// <summary>
/// Thrown when a sale asks for more stock than is available (FR5).
/// The sale is cancelled and nothing is saved.
/// </summary>
public class InsufficientStockException : Exception
{
    public int ProductId { get; }

    public InsufficientStockException(int productId, string message) : base(message)
    {
        ProductId = productId;
    }
}