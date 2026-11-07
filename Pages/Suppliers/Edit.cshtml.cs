using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Repositories;

namespace StockSmart.Pages.Suppliers;

/// <summary>
/// Edit a supplier (FR3). Manager only: protected by the ManagerOnly rule in Program.cs (NFR1).
/// Opened as /Suppliers/Edit?id=2.
/// </summary>
public class EditModel : PageModel
{
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<EditModel> _logger;

    public EditModel(SupplierRepository supplierRepository, ILogger<EditModel> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    [BindProperty]
    public SupplierInput Input { get; set; } = new();

    public int SupplierId { get; private set; }
    public string OriginalName { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            SupplierId = id;
            OriginalName = supplier.Name;
            Input = SupplierInput.FromSupplier(supplier);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading supplier {SupplierId} failed.", id);
            ErrorMessage = "The supplier could not be loaded. Please try again.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Input.Tidy();

        try
        {
            var existing = await _supplierRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            SupplierId = id;
            OriginalName = existing.Name;

            // Duplicate check excludes this supplier, so saving without renaming is allowed.
            if (ModelState.IsValid && await _supplierRepository.NameExistsAsync(Input.Name, id))
            {
                ModelState.AddModelError("Input.Name", $"A supplier called \"{Input.Name}\" already exists.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _supplierRepository.UpdateAsync(Input.ToSupplier(id));
            Message = $"Supplier \"{Input.Name}\" was updated.";
            return RedirectToPage("Index");
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Updating supplier {SupplierId} failed.", id);
            ErrorMessage = "The supplier could not be saved. Please try again.";
            return Page();
        }
    }
}