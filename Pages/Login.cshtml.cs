using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Services;

namespace StockSmart.Pages;

/// <summary>Login page (FR1.1). Signs the user in with a cookie that holds their ID, name and role.</summary>
public class LoginModel : PageModel
{
    private readonly AuthService _auth;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(AuthService auth, ILogger<LoginModel> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    /// <summary>Shown above the form when the login fails.</summary>
    public string? ErrorMessage { get; set; }

    public class LoginInput
    {
        [Required(ErrorMessage = "Enter your username.")]
        [StringLength(50, ErrorMessage = "Username can be at most 50 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        // Already logged in: go straight to the home page.
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Server-side check, in case JavaScript is switched off.
        if (!ModelState.IsValid)
            return Page();

        AppUser? user;
        try
        {
            user = await _auth.AuthenticateAsync(Input.Username, Input.Password);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Login failed because the database could not be reached.");
            ErrorMessage = "Cannot connect to the database. Please try again later.";
            return Page();
        }

        if (user is null)
        {
            // One message for every failure, so usernames can't be guessed (NFR1).
            _logger.LogWarning("Failed login attempt for username '{Username}'.", Input.Username);
            ErrorMessage = "Invalid username or password.";
            Input.Password = string.Empty;
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        _logger.LogInformation("User '{Username}' ({Role}) logged in.", user.Username, user.Role);

        // Only follow return links inside this site, never to another website.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Index");
    }
}