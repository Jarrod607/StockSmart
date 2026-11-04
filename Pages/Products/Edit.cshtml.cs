using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockSmart.Exceptions;
using StockSmart.Repositories;

namespace StockSmart.Pages.Products;

/// <summary>
/// Edit a product (FR2.1). Manager only: protected by the ManagerOnly rule in Program.cs (NFR1).
/// Opened as /Products/Edit?id=5.
/// </summary>
public class EditModel : PageModel
{
    private readonly ProductRepository _productRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<EditModel> _logger;

    public EditModel(ProductRepository productRepository, SupplierRepository supplierRepository,
                     ILogger<EditModel> logger)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    [BindProperty]
    public ProductInput Input { get; set; } = new();

    public int ProductId { get; private set; }
    public string OriginalName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public SelectList SupplierOptions { get; private set; } = new(Array.Empty<object>());
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ProductId = id;
            OriginalName = product.Name;
            IsActive = product.IsActive;
            Input = ProductInput.FromProduct(product);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading product {ProductId} failed.", id);
            ErrorMessage = "The product could not be loaded. Please try again.";
        }

        await LoadSuppliersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        ProductId = id;
        Input.Tidy();

        try
        {
            var existing = await _productRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }
            OriginalName = existing.Name;
            IsActive = existing.IsActive;

            // Duplicate-name check, ignoring this product's own name.
            if (ModelState.IsValid && await _productRepository.NameExistsAsync(Input.Name, id))
            {
                ModelState.AddModelError("Input.Name", $"A product called \"{Input.Name}\" already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSuppliersAsync();
                return Page();
            }

            var product = Input.ToProduct(id);
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation("Product {ProductId} '{Name}' updated by {User}.",
                id, product.Name, User.Identity?.Name);

            TempData["Message"] = $"Product \"{product.Name}\" was updated.";
            return RedirectToPage("Index");
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Updating product {ProductId} failed.", id);
            ErrorMessage = "The changes could not be saved. Please try again.";
            await LoadSuppliersAsync();
            return Page();
        }
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            var suppliers = await _supplierRepository.GetAllAsync();
            SupplierOptions = new SelectList(suppliers, "SupplierId", "Name", Input.SupplierId);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading suppliers failed.");
            ErrorMessage = "Suppliers could not be loaded. Please try again.";
        }
    }
}