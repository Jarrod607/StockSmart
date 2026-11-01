using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StockSmart.Pages;

/// <summary>
/// Shown when a logged-in user opens a page their role doesn't allow (NFR1).
/// Every refused attempt is logged.
/// </summary>
public class AccessDeniedModel : PageModel
{
    private readonly ILogger<AccessDeniedModel> _logger;

    public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
    {
        _logger = logger;
    }

    public string Role { get; private set; } = string.Empty;

    public void OnGet(string? returnUrl)
    {
        Role = User.FindFirstValue(ClaimTypes.Role) ?? "Unknown";
        _logger.LogWarning("Access denied: user '{Username}' ({Role}) tried to open '{ReturnUrl}'.",
            User.Identity?.Name, Role, returnUrl);
    }
}