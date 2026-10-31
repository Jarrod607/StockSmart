using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Models;

namespace StockSmart.Repositories;

/// <summary>Reads suppliers from the Supplier table.</summary>
public class SupplierRepository : RepositoryBase
{
    public SupplierRepository(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>Returns all suppliers, sorted by name.</summary>
    public async Task<List<Supplier>> GetAllAsync()
    {
        const string sql =
            @"SELECT SupplierId, Name, Phone, Email
              FROM dbo.Supplier
              ORDER BY Name;";

        var suppliers = new List<Supplier>();
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
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