using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Models;

namespace StockSmart.Repositories;

/// <summary>Reads and writes users in the AppUser table. All SQL is parameterised (NFR1).</summary>
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

    /// <summary>
    /// Returns all users, sorted by username, for the Users page (FR1.3).
    /// Password hashes are deliberately not read: the page never needs them.
    /// </summary>
    public async Task<List<AppUser>> GetAllAsync()
    {
        const string sql =
            @"SELECT UserId, Username, Role, IsActive
              FROM dbo.AppUser
              ORDER BY Username;";

        var users = new List<AppUser>();
        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new AppUser
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Username = reader.GetString(reader.GetOrdinal("Username")),
                    Role = reader.GetString(reader.GetOrdinal("Role")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return users;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not read the list of users from the database.", ex);
        }
    }

    /// <summary>Returns true if a user with this username already exists.</summary>
    public async Task<bool> UsernameExistsAsync(string username)
    {
        const string sql =
            @"SELECT COUNT(*)
              FROM dbo.AppUser
              WHERE Username = @username;";

        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@username", username);

            int count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not check the username in the database.", ex);
        }
    }

    /// <summary>
    /// Adds a new, active user and returns the new UserId.
    /// The caller passes a password HASH, never the plain password.
    /// </summary>
    public async Task<int> AddAsync(string username, string passwordHash, string role)
    {
        const string sql =
            @"INSERT INTO dbo.AppUser (Username, PasswordHash, Role, IsActive)
              OUTPUT INSERTED.UserId
              VALUES (@username, @passwordHash, @role, 1);";

        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@role", role);

            return (int)(await command.ExecuteScalarAsync())!;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not add the user to the database.", ex);
        }
    }

    /// <summary>
    /// Deactivates (false) or reactivates (true) a user. Users are never deleted,
    /// so past sales keep their link to the user who made them.
    /// Returns false if no user has this ID.
    /// </summary>
    public async Task<bool> SetActiveAsync(int userId, bool isActive)
    {
        const string sql =
            @"UPDATE dbo.AppUser
              SET IsActive = @isActive
              WHERE UserId = @userId;";

        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@isActive", isActive);
            command.Parameters.AddWithValue("@userId", userId);

            return await command.ExecuteNonQueryAsync() == 1;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not change the user's status in the database.", ex);
        }
    }

    /// <summary>Saves a new password hash for a user. Returns false if no user has this ID.</summary>
    public async Task<bool> ResetPasswordAsync(int userId, string newPasswordHash)
    {
        const string sql =
            @"UPDATE dbo.AppUser
              SET PasswordHash = @passwordHash
              WHERE UserId = @userId;";

        try
        {
            using var connection = await OpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@passwordHash", newPasswordHash);
            command.Parameters.AddWithValue("@userId", userId);

            return await command.ExecuteNonQueryAsync() == 1;
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not reset the user's password in the database.", ex);
        }
    }
}