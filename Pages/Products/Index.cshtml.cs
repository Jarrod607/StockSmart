using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages.Products;

/// <summary>
/// Product list (FR2). Every logged-in user can view it;
/// only the Manager sees the Add and Edit buttons (NFR1).
/// </summary>
public class IndexModel : PageModel
{
    private readonly ProductRepository _productRepository;
    private readonly SupplierRepository _supplierRepository;

    public IndexModel(ProductRepository productRepository, SupplierRepository supplierRepository)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
    }

    public List<Product> ProductList { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    // SupplierId -> supplier name, so each row can show who supplies it.
    private Dictionary<int, string> _supplierNames = new();

    public async Task OnGetAsync()
    {
        try
        {
            ProductList = await _productRepository.GetAllAsync();
            var suppliers = await _supplierRepository.GetAllAsync();
            _supplierNames = suppliers.ToDictionary(s => s.SupplierId, s => s.Name);
        }
        catch (DatabaseException)
        {
            ErrorMessage = "Products could not be loaded. Please try again.";
        }
    }

    public string SupplierName(int supplierId) =>
        _supplierNames.TryGetValue(supplierId, out var name) ? name : "—";
}