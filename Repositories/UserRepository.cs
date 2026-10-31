using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Models;

namespace StockSmart.Repositories;

/// <summary>Reads users from the AppUser table. All SQL is parameterised (NFR1).</summary>
public class UserRepository : RepositoryBase
{
    public UserRepository(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>Finds a user by username. Returns null if no such user exists.</summary>
    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        const string sql =
            @"SELECT UserId, Username, PasswordHash, Role, IsActive
              FROM dbo.AppUser
              WHERE Username = @username;";

        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@username", username);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new AppUser
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not read the user from the database.", ex);
        }
    }
}