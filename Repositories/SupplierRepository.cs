using System.Data;
using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Models;

namespace StockSmart.Repositories;

/// <summary>Reads and saves suppliers (FR3). All SQL is parameterised (NFR1).</summary>
public class SupplierRepository : RepositoryBase
{
    private const string SelectColumns = "SELECT SupplierId, Name, Phone, Email FROM dbo.Supplier";

    public SupplierRepository(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>Returns all suppliers, sorted by name.</summary>
    public async Task<List<Supplier>> GetAllAsync()
    {
        return await QueryAsync(SelectColumns + " ORDER BY Name;", null);
    }

    /// <summary>Finds one supplier by its ID. Returns null if it doesn't exist.</summary>
    public async Task<Supplier?> GetByIdAsync(int supplierId)
    {
        var results = await QueryAsync(SelectColumns + " WHERE SupplierId = @supplierId;",
            command => command.Parameters.Add("@supplierId", SqlDbType.Int).Value = supplierId);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// True if another supplier already has this name. On Edit, pass the supplier's own ID
    /// so saving without renaming isn't flagged as a duplicate.
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, int? excludeSupplierId = null)
    {
        const string sql =
            @"SELECT COUNT(*) FROM dbo.Supplier
              WHERE Name = @name AND (@excludeId IS NULL OR SupplierId <> @excludeId);";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = name;
            command.Parameters.Add("@excludeId", SqlDbType.Int).Value =
                (object?)excludeSupplierId ?? DBNull.Value;
            return (int)(await command.ExecuteScalarAsync())! > 0;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not check the supplier name.", ex);
        }
    }

    /// <summary>Adds a supplier and returns its new ID.</summary>
    public async Task<int> AddAsync(Supplier supplier)
    {
        const string sql =
            @"INSERT INTO dbo.Supplier (Name, Phone, Email)
              OUTPUT INSERTED.SupplierId
              VALUES (@name, @phone, @email);";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            AddFields(command, supplier);
            return (int)(await command.ExecuteScalarAsync())!;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not add the supplier.", ex);
        }
    }

    /// <summary>Updates a supplier's name, phone and email.</summary>
    public async Task UpdateAsync(Supplier supplier)
    {
        const string sql =
            @"UPDATE dbo.Supplier
              SET Name = @name, Phone = @phone, Email = @email
              WHERE SupplierId = @supplierId;";
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            AddFields(command, supplier);
            command.Parameters.Add("@supplierId", SqlDbType.Int).Value = supplier.SupplierId;
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not update the supplier.", ex);
        }
    }

    /// <summary>Adds the shared fields. Empty phone or email is stored as NULL.</summary>
    private static void AddFields(SqlCommand command, Supplier supplier)
    {
        command.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = supplier.Name;
        command.Parameters.Add("@phone", SqlDbType.NVarChar, 20).Value =
            (object?)supplier.Phone ?? DBNull.Value;
        command.Parameters.Add("@email", SqlDbType.NVarChar, 100).Value =
            (object?)supplier.Email ?? DBNull.Value;
    }

    /// <summary>Runs a SELECT and turns each row into a Supplier.</summary>
    private async Task<List<Supplier>> QueryAsync(string sql, Action<SqlCommand>? addParameters)
    {
        var suppliers = new List<Supplier>();
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            addParameters?.Invoke(command);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int phone = reader.GetOrdinal("Phone");
                int email = reader.GetOrdinal("Email");
                suppliers.Add(new Supplier
                {
                    SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Phone = reader.IsDBNull(phone) ? null : reader.GetString(phone),
                    Email = reader.IsDBNull(email) ? null : reader.GetString(email)
                });
            }
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not read suppliers from the database.", ex);
        }
        return suppliers;
    }
}