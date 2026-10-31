using Microsoft.Data.SqlClient;

namespace StockSmart.Repositories;

/// <summary>
/// Shared code for all repositories: reads the connection string once
/// and opens connections to the StockSmart database.
/// </summary>
public abstract class RepositoryBase
{
    private readonly string _connectionString;

    protected RepositoryBase(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("StockSmart")
            ?? throw new InvalidOperationException("Connection string 'StockSmart' is missing from appsettings.json.");
    }

    /// <summary>Opens a new connection. The caller disposes it with 'using'.</summary>
    protected async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}