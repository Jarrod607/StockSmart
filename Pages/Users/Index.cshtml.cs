using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages.Users;

/// <summary>
/// Lists all users for the Manager (FR1.3).
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

    public string? ErrorMessage { get; private set; }

    /// <summary>A success message passed from another page, e.g. "User added."</summary>
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
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
}