using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;
using StockSmart.Security;

namespace StockSmart.Pages.Users;

/// <summary>
/// Lets the Manager set a new password for a user (FR1.3).
/// The new password is hashed before saving; the old one can't be recovered, only replaced (NFR1).
/// </summary>
public class ResetPasswordModel : PageModel
{
    private readonly UserRepository _users;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(UserRepository users, ILogger<ResetPasswordModel> logger)
    {
        _users = users;
        _logger = logger;
    }

    /// <summary>The UserId from the link, e.g. /Users/ResetPassword?id=4</summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public string Username { get; private set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Enter a new password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "The password must contain at least one letter and one number.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            AppUser? user = await FindUserAsync();
            if (user is null)
            {
                TempData["WarningMessage"] = "That user could not be found.";
                return RedirectToPage("/Users/Index");
            }

            Username = user.Username;
            return Page();
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Could not load user {UserId} for a password reset.", Id);
            ErrorMessage = "Cannot connect to the database. Please try again later.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            AppUser? user = await FindUserAsync();
            if (user is null)
            {
                TempData["WarningMessage"] = "That user could not be found.";
                return RedirectToPage("/Users/Index");
            }

            Username = user.Username;

            if (!ModelState.IsValid)
                return Page();

            string hash = PasswordHasher.Hash(Input.NewPassword);
            await _users.ResetPasswordAsync(Id, hash);

            _logger.LogInformation("Manager '{Manager}' reset the password for user '{Username}'.",
                User.Identity?.Name, user.Username);

            TempData["StatusMessage"] = $"The password for '{user.Username}' was reset.";
            return RedirectToPage("/Users/Index");
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Could not reset the password for user {UserId}.", Id);
            ErrorMessage = "Cannot connect to the database. Please try again later.";
            return Page();
        }
    }

    private async Task<AppUser?> FindUserAsync() =>
        (await _users.GetAllAsync()).FirstOrDefault(u => u.UserId == Id);
}