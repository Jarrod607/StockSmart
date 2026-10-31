using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Models;

namespace StockSmart.Repositories;

/// <summary>Reads products from the Product table. All SQL is parameterised (NFR1).</summary>
public class ProductRepository : RepositoryBase
{
    private const string SelectColumns =
        "SELECT ProductId, Name, Category, Price, Quantity, ReorderLevel, IsActive, SupplierId FROM dbo.Product";

    public ProductRepository(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>Returns all active products, sorted by name.</summary>
    public async Task<List<Product>> GetAllAsync()
    {
        string sql = SelectColumns + " WHERE IsActive = 1 ORDER BY Name;";
        return await QueryAsync(sql, null);
    }

    /// <summary>Finds one product by its ID. Returns null if it doesn't exist.</summary>
    public async Task<Product?> GetByIdAsync(int productId)
    {
        string sql = SelectColumns + " WHERE ProductId = @productId;";
        var results = await QueryAsync(sql, command =>
            command.Parameters.AddWithValue("@productId", productId));
        return results.FirstOrDefault();
    }

    /// <summary>Searches active products by name or category (FR2.2).</summary>
    public async Task<List<Product>> SearchAsync(string searchTerm)
    {
        string sql = SelectColumns +
            " WHERE IsActive = 1 AND (Name LIKE @term OR Category LIKE @term) ORDER BY Name;";
        return await QueryAsync(sql, command =>
            command.Parameters.AddWithValue("@term", "%" + searchTerm + "%"));
    }

    /// <summary>Runs a SELECT and turns each row into a Product.</summary>
    private async Task<List<Product>> QueryAsync(string sql, Action<SqlCommand>? addParameters)
    {
        var products = new List<Product>();
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            addParameters?.Invoke(command);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int category = reader.GetOrdinal("Category");
                products.Add(new Product
                {
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Category = reader.IsDBNull(category) ? null : reader.GetString(category),
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    ReorderLevel = reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId"))
                });
            }
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not read products from the database.", ex);
        }
        return products;
    }
}