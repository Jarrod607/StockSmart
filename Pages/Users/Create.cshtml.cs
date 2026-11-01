using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;
using StockSmart.Security;

namespace StockSmart.Pages.Users;

/// <summary>
/// Lets the Manager add a new user (FR1.3).
/// The password is hashed before it reaches the database; the plain password is never stored (NFR1).
/// </summary>
public class CreateModel : PageModel
{
    private static readonly string[] AllowedRoles = { UserRoles.Staff, UserRoles.Manager, UserRoles.Owner };

    private readonly UserRepository _users;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(UserRepository users, ILogger<CreateModel> logger)
    {
        _users = users;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public SelectList RoleOptions { get; } = new(AllowedRoles);

    public class InputModel
    {
        [Required(ErrorMessage = "Enter a username.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "The username must be 3 to 50 characters.")]
        [RegularExpression(@"^[A-Za-z0-9._]+$", ErrorMessage = "Use only letters, numbers, dots and underscores.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "The password must contain at least one letter and one number.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the password.")]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a role.")]
        public string Role { get; set; } = UserRoles.Staff;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // The browser checks the form first; these checks run again on the server.
        if (!AllowedRoles.Contains(Input.Role))
            ModelState.AddModelError("Input.Role", "Choose a valid role.");

        if (!ModelState.IsValid)
            return Page();

        string username = Input.Username.Trim();

        try
        {
            if (await _users.UsernameExistsAsync(username))
            {
                ModelState.AddModelError("Input.Username", "That username is already taken.");
                return Page();
            }

            string hash = PasswordHasher.Hash(Input.Password);
            int newId = await _users.AddAsync(username, hash, Input.Role);

            _logger.LogInformation("Manager '{Manager}' added user '{Username}' ({Role}) with ID {UserId}.",
                User.Identity?.Name, username, Input.Role, newId);

            TempData["StatusMessage"] = $"User '{username}' was added as {Input.Role}.";
            return RedirectToPage("Index");
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Could not add user '{Username}'.", username);
            ErrorMessage = "Cannot connect to the database. Please try again later.";
            return Page();
        }
    }
}