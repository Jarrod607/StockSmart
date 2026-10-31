using StockSmart.Models;
using StockSmart.Repositories;
using StockSmart.Security;

namespace StockSmart.Services;

/// <summary>
/// Checks a username and password at login (FR1.1).
/// Every failure returns null, so the page shows one message for all of them
/// and never reveals whether the username exists (NFR1).
/// </summary>
public class AuthService
{
    private readonly UserRepository _users;

    public AuthService(UserRepository users)
    {
        _users = users;
    }

    /// <summary>
    /// Returns the user if the login is valid, otherwise null.
    /// A DatabaseException is passed on to the page, which shows a connection message.
    /// </summary>
    public async Task<AppUser?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        AppUser? user = await _users.GetByUsernameAsync(username.Trim());

        if (user is null)
            return null;            // Unknown username

        if (!user.IsActive)
            return null;            // Account deactivated by the Manager

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            return null;            // Wrong password

        return user;
    }
}