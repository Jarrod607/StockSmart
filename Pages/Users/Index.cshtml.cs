using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages.Users;

/// <summary>
/// Lists all users and lets the Manager deactivate or reactivate them (FR1.3).
/// Only the Manager can open this page: see the ManagerOnly rule in Program.cs.
/// </summary>
public class IndexModel : PageModel
{
    private readonly UserRepository _users;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(UserRepository users, ILogger<IndexModel> logger)
    {
        _users = users;
        _logger = logger;
    }

    public List<AppUser> Users { get; private set; } = new();

    /// <summary>The logged-in Manager's own UserId, so the page can protect their row.</summary>
    public int CurrentUserId { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>A success message passed between pages, e.g. "User added."</summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>A warning passed between pages, e.g. "You can't deactivate your own account."</summary>
    [TempData]
    public string? WarningMessage { get; set; }

    public async Task OnGetAsync()
    {
        CurrentUserId = GetCurrentUserId();
        try
        {
            Users = await _users.GetAllAsync();
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Could not load the user list.");
            ErrorMessage = "Cannot connect to the database. Please try again later.";
        }
    }

    /// <summary>
    /// Deactivates (active = false) or reactivates (active = true) a user.
    /// A Manager can never deactivate their own account; this is checked here on the server,
    /// not only by hiding the button.
    /// </summary>
    public async Task<IActionResult> OnPostSetActiveAsync(int id, bool active)
    {
        if (id == GetCurrentUserId() && !active)
        {
            _logger.LogWarning("Manager '{Manager}' tried to deactivate their own account.", User.Identity?.Name);
            WarningMessage = "You can't deactivate your own account.";
            return RedirectToPage();
        }

        try
        {
            AppUser? target = (await _users.GetAllAsync()).FirstOrDefault(u => u.UserId == id);
            if (target is null || !await _users.SetActiveAsync(id, active))
            {
                WarningMessage = "That user could not be found.";
                return RedirectToPage();
            }

            _logger.LogInformation("Manager '{Manager}' {Action} user '{Username}'.",
                User.Identity?.Name, active ? "reactivated" : "deactivated", target.Username);

            StatusMessage = active
                ? $"User '{target.Username}' was reactivated."
                : $"User '{target.Username}' was deactivated and can no longer log in.";
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Could not change the status of user {UserId}.", id);
            WarningMessage = "Cannot connect to the database. Please try again later.";
        }

        return RedirectToPage();
    }

    /// <summary>Reads the logged-in user's ID from the login cookie (set at login).</summary>
    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : 0;
}