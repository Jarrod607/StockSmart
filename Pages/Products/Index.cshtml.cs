using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages.Products;

/// <summary>
/// Product list with search (FR2, FR2.2). Every logged-in user can view it;
/// only the Manager sees the Add and Edit buttons (NFR1).
/// </summary>
public class IndexModel : PageModel
{
    private const int MaxSearchLength = 100;

    private readonly ProductRepository _productRepository;
    private readonly SupplierRepository _supplierRepository;

    public IndexModel(ProductRepository productRepository, SupplierRepository supplierRepository)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
    }

    /// <summary>Search text from the URL (?search=...). Matches name or category.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
/// <summary>Manager only: include deactivated products (?showInactive=true).</summary>
    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }
    public List<Product> ProductList { get; private set; } = new();
    public string? ErrorMessage { get; private set; }
    public bool IsSearching => !string.IsNullOrWhiteSpace(Search);

    // SupplierId -> supplier name, so each row can show who supplies it.
    private Dictionary<int, string> _supplierNames = new();

    public async Task OnGetAsync()
    {
        try
        {
            if (IsSearching)
            {
                string term = Search!.Trim();
                if (term.Length > MaxSearchLength)
                {
                    term = term.Substring(0, MaxSearchLength);
                }
                Search = term;
                ProductList = await _productRepository.SearchAsync(term);   // parameterised (NFR1)
            }
                        else if (ShowInactive && User.IsInRole(UserRoles.Manager))
            {
                ProductList = await _productRepository.GetAllIncludingInactiveAsync();
            }
            else
            {
                ShowInactive = false;   // Staff can't see inactive products, even via the URL (NFR1).
                ProductList = await _productRepository.GetAllAsync();
            }

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