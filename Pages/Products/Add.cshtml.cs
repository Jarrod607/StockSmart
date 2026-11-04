using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockSmart.Exceptions;
using StockSmart.Repositories;

namespace StockSmart.Pages.Products;

/// <summary>
/// Add a product (FR2.1). Manager only: protected by the ManagerOnly rule in Program.cs (NFR1).
/// </summary>
public class AddModel : PageModel
{
    private readonly ProductRepository _productRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<AddModel> _logger;

    public AddModel(ProductRepository productRepository, SupplierRepository supplierRepository,
                    ILogger<AddModel> logger)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    [BindProperty]
    public ProductInput Input { get; set; } = new();

    public SelectList SupplierOptions { get; private set; } = new(Array.Empty<object>());
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadSuppliersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Tidy();

        try
        {
            // Duplicate-name check (UQ_Product_Name), only once the basic rules pass.
            if (ModelState.IsValid && await _productRepository.NameExistsAsync(Input.Name))
            {
                ModelState.AddModelError("Input.Name", $"A product called \"{Input.Name}\" already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSuppliersAsync();
                return Page();
            }

            var product = Input.ToProduct();
            int newId = await _productRepository.AddAsync(product);

            _logger.LogInformation("Product {ProductId} '{Name}' added by {User}.",
                newId, product.Name, User.Identity?.Name);

            TempData["Message"] = $"Product \"{product.Name}\" was added.";
            return RedirectToPage("Index");
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Adding product '{Name}' failed.", Input.Name);
            ErrorMessage = "The product could not be saved. Please try again.";
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