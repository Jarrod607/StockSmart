using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages;

/// <summary>
/// Home dashboard: product, low-stock and supplier counts, plus quick links.
/// Every logged-in user sees it.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ProductRepository _productRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ProductRepository productRepository, SupplierRepository supplierRepository,
                      ILogger<IndexModel> logger)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    public int ProductCount { get; private set; }
    public int SupplierCount { get; private set; }
    public List<Product> LowStock { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();   // active products only
            ProductCount = products.Count;
            LowStock = products
                .Where(p => p.IsLowStock)
                .OrderBy(p => p.Quantity)
                .ToList();
            SupplierCount = (await _supplierRepository.GetAllAsync()).Count;
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading the dashboard failed.");
            ErrorMessage = "The dashboard figures could not be loaded. Please try again.";
        }
    }
}