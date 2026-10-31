namespace StockSmart.Models;

/// <summary>A system user (AppUser table). Only the password hash is stored, never the password.</summary>
public class AppUser
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Staff;
    public bool IsActive { get; set; } = true;
}

/// <summary>The three roles allowed by CK_AppUser_Role.</summary>
public static class UserRoles
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
}