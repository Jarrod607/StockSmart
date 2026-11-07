using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages.Suppliers;

/// <summary>
/// One supplier and the active products it supplies (FR3: the Supplier–Product link).
/// Opened as /Suppliers/Details?id=2.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly SupplierRepository _supplierRepository;
    private readonly ProductRepository _productRepository;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(SupplierRepository supplierRepository, ProductRepository productRepository,
                        ILogger<DetailsModel> logger)
    {
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public Supplier? Supplier { get; private set; }
    public List<Product> ProductList { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Supplier = await _supplierRepository.GetByIdAsync(id);
            if (Supplier == null)
            {
                return NotFound();
            }

            ProductList = (await _productRepository.GetAllAsync())
                .Where(p => p.SupplierId == id)
                .ToList();
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading supplier {SupplierId} failed.", id);
            ErrorMessage = "The supplier could not be loaded. Please try again.";
        }
        return Page();
    }
}