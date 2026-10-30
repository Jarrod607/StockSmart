namespace StockSmart.Exceptions;

/// <summary>
/// Raised when the database cannot be reached or a database operation fails.
/// Wraps the original SqlException so the cause is never lost.
/// </summary>
public class DatabaseException : Exception
{
    public DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
