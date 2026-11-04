using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Models;

namespace StockSmart.Repositories;

/// <summary>
/// Saves sales (FR4, FR5). A sale, its lines and the stock reductions are saved in ONE
/// transaction: either everything is saved, or nothing is (NFR2).
/// All SQL is parameterised (NFR1).
/// </summary>
public class SaleRepository : RepositoryBase
{
    public SaleRepository(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Saves the sale and reduces stock for every line. Returns the new SaleId.
    /// If any line fails, everything is rolled back and an exception is thrown.
    /// </summary>
    public async Task<int> CreateSaleAsync(Sale sale)
    {
        if (sale.Items.Count == 0)
        {
            throw new ArgumentException("A sale needs at least one line.", nameof(sale));
        }

        try
        {
            using var connection = await OpenConnectionAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                int saleId = await InsertSaleAsync(connection, transaction, sale);

                foreach (var item in sale.Items)
                {
                    await ReduceStockAsync(connection, transaction, item);
                    await InsertSaleItemAsync(connection, transaction, saleId, item);
                }

                transaction.Commit();
                return saleId;
            }
            catch
            {
                // Undo everything this sale did: the Sale row, any SaleItems and any stock reductions.
                transaction.Rollback();
                throw;
            }
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("The sale could not be saved. Nothing was changed.", ex);
        }
    }

    /// <summary>Counts the rows in Sale and SaleItem (used to prove a rollback left nothing behind).</summary>
    public async Task<(int Sales, int SaleItems)> CountAsync()
    {
        const string sql = "SELECT (SELECT COUNT(*) FROM dbo.Sale), (SELECT COUNT(*) FROM dbo.SaleItem);";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (reader.GetInt32(0), reader.GetInt32(1));
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not count sales.", ex);
        }
    }

    private static async Task<int> InsertSaleAsync(SqlConnection connection, SqlTransaction transaction, Sale sale)
    {
        const string sql =
            "INSERT INTO dbo.Sale (SaleDate, Total, UserId) " +
            "OUTPUT INSERTED.SaleId " +
            "VALUES (@saleDate, @total, @userId);";
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@saleDate", sale.SaleDate);
        command.Parameters.AddWithValue("@total", sale.Total);
        command.Parameters.AddWithValue("@userId", sale.UserId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Checks and reduces stock in ONE statement: the row only changes if enough stock is left,
    /// so two people selling the last item at the same moment can't both succeed (FR5).
    /// </summary>
    private static async Task ReduceStockAsync(SqlConnection connection, SqlTransaction transaction, SaleItem item)
    {
        const string sql =
            "UPDATE dbo.Product SET Quantity = Quantity - @quantity " +
            "WHERE ProductId = @productId AND IsActive = 1 AND Quantity >= @quantity;";
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@quantity", item.Quantity);
        command.Parameters.AddWithValue("@productId", item.ProductId);

        int rowsChanged = await command.ExecuteNonQueryAsync();
        if (rowsChanged == 0)
        {
            throw new InsufficientStockException(item.ProductId,
                $"Product {item.ProductId} doesn't have enough stock, or is inactive. The sale was cancelled.");
        }
    }

    private static async Task InsertSaleItemAsync(SqlConnection connection, SqlTransaction transaction,
                                                  int saleId, SaleItem item)
    {
        const string sql =
            "INSERT INTO dbo.SaleItem (SaleId, ProductId, Quantity, UnitPrice) " +
            "VALUES (@saleId, @productId, @quantity, @unitPrice);";
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@saleId", saleId);
        command.Parameters.AddWithValue("@productId", item.ProductId);
        command.Parameters.AddWithValue("@quantity", item.Quantity);
        command.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
        await command.ExecuteNonQueryAsync();
    }
}