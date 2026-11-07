using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages.Suppliers;

/// <summary>
/// Supplier list (FR3). Everyone logged in can view it; only the Manager sees Add and Edit.
/// </summary>
public class IndexModel : PageModel
{
    private readonly SupplierRepository _supplierRepository;
    private readonly ProductRepository _productRepository;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SupplierRepository supplierRepository, ProductRepository productRepository,
                      ILogger<IndexModel> logger)
    {
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public List<Supplier> SupplierList { get; private set; } = new();
    public Dictionary<int, int> ProductCounts { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    /// <summary>Success message from Add or Edit, carried across the redirect.</summary>
    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            SupplierList = await _supplierRepository.GetAllAsync();
            var products = await _productRepository.GetAllAsync();   // active products only
            ProductCounts = products
                .GroupBy(p => p.SupplierId)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading suppliers failed.");
            ErrorMessage = "The suppliers could not be loaded. Please try again.";
        }
    }

    public int CountFor(int supplierId) => ProductCounts.GetValueOrDefault(supplierId);
}