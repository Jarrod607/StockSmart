using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages;

/// <summary>
/// Low-stock alert (FR6): every active product at or below its reorder level,
/// with its supplier's contact details so the Manager knows who to call.
/// </summary>
public class LowStockModel : PageModel
{
    private readonly ProductRepository _productRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<LowStockModel> _logger;

    public LowStockModel(ProductRepository productRepository, SupplierRepository supplierRepository,
                         ILogger<LowStockModel> logger)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    public List<Product> Items { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    private Dictionary<int, Supplier> _suppliers = new();

    public async Task OnGetAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();   // active products only
            Items = products
                .Where(p => p.IsLowStock)
                .OrderBy(p => p.Quantity - p.ReorderLevel)          // furthest below first
                .ThenBy(p => p.Name)
                .ToList();

            var suppliers = await _supplierRepository.GetAllAsync();
            _suppliers = suppliers.ToDictionary(s => s.SupplierId);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading the low-stock list failed.");
            ErrorMessage = "The low-stock list could not be loaded. Please try again.";
        }
    }

    public Supplier? SupplierFor(int supplierId) =>
        _suppliers.TryGetValue(supplierId, out var s) ? s : null;
}