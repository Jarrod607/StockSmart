using System.Data;
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
    /// <summary>Returns every product, including deactivated ones (Manager view, FR2.1).</summary>
    public async Task<List<Product>> GetAllIncludingInactiveAsync()
    {
        string sql = SelectColumns + " ORDER BY IsActive DESC, Name;";
        return await QueryAsync(sql, null);
    }

    /// <summary>
    /// True if another product already has this name (UQ_Product_Name).
    /// Pass excludeProductId when editing, so a product doesn't clash with itself.
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, int? excludeProductId = null)
    {
        const string sql =
            "SELECT COUNT(*) FROM dbo.Product " +
            "WHERE Name = @name AND (@excludeId IS NULL OR ProductId <> @excludeId);";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.Add("@excludeId", SqlDbType.Int).Value =
                (object?)excludeProductId ?? DBNull.Value;

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not check the product name.", ex);
        }
    }

    /// <summary>Adds a new, active product and returns its new ProductId (FR2.1).</summary>
    public async Task<int> AddAsync(Product product)
    {
        const string sql =
            "INSERT INTO dbo.Product (Name, Category, Price, Quantity, ReorderLevel, IsActive, SupplierId) " +
            "OUTPUT INSERTED.ProductId " +
            "VALUES (@name, @category, @price, @quantity, @reorderLevel, 1, @supplierId);";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            AddProductParameters(command, product);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not add the product.", ex);
        }
    }

    /// <summary>Saves changes to an existing product (FR2.1).</summary>
    public async Task UpdateAsync(Product product)
    {
        const string sql =
            "UPDATE dbo.Product SET Name = @name, Category = @category, Price = @price, " +
            "Quantity = @quantity, ReorderLevel = @reorderLevel, SupplierId = @supplierId " +
            "WHERE ProductId = @productId;";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            AddProductParameters(command, product);
            command.Parameters.AddWithValue("@productId", product.ProductId);
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not update the product.", ex);
        }
    }

    /// <summary>
    /// Deactivates or reactivates a product. Products are never deleted,
    /// so past sales stay linked to them (FR2.1).
    /// </summary>
    public async Task SetActiveAsync(int productId, bool isActive)
    {
        const string sql = "UPDATE dbo.Product SET IsActive = @isActive WHERE ProductId = @productId;";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@isActive", isActive);
            command.Parameters.AddWithValue("@productId", productId);
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not change the product's status.", ex);
        }
    }

    /// <summary>The parameters shared by AddAsync and UpdateAsync.</summary>
    private static void AddProductParameters(SqlCommand command, Product product)
    {
        command.Parameters.AddWithValue("@name", product.Name);
        command.Parameters.AddWithValue("@category", (object?)product.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@price", product.Price);
        command.Parameters.AddWithValue("@quantity", product.Quantity);
        command.Parameters.AddWithValue("@reorderLevel", product.ReorderLevel);
        command.Parameters.AddWithValue("@supplierId", product.SupplierId);
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