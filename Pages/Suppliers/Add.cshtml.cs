using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Repositories;

namespace StockSmart.Pages.Suppliers;

/// <summary>
/// Add a supplier (FR3). Manager only: protected by the ManagerOnly rule in Program.cs (NFR1).
/// </summary>
public class AddModel : PageModel
{
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<AddModel> _logger;

    public AddModel(SupplierRepository supplierRepository, ILogger<AddModel> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    [BindProperty]
    public SupplierInput Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    /// <summary>Success message, shown on the Suppliers list after the redirect.</summary>
    [TempData]
    public string? Message { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Tidy();

        try
        {
            // Duplicate-name check, only once the basic rules pass.
            if (ModelState.IsValid && await _supplierRepository.NameExistsAsync(Input.Name))
            {
                ModelState.AddModelError("Input.Name", $"A supplier called \"{Input.Name}\" already exists.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _supplierRepository.AddAsync(Input.ToSupplier());
            Message = $"Supplier \"{Input.Name}\" was added.";
            return RedirectToPage("Index");
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Adding supplier {Name} failed.", Input.Name);
            ErrorMessage = "The supplier could not be saved. Please try again.";
            return Page();
        }
    }
}