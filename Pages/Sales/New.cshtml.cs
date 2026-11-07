using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;
using StockSmart.Services;

namespace StockSmart.Pages.Sales;

/// <summary>
/// New Sale page (FR4, FR5). Staff and Manager build a sale line by line.
/// SaleService applies the rules, and SaleRepository saves it in one transaction (NFR2).
/// The total shown on screen is a preview only; the saved total is calculated by SaleService.
/// </summary>
public class NewModel : PageModel
{
    private readonly ProductRepository _productRepository;
    private readonly SaleService _saleService;

    public NewModel(ProductRepository productRepository, SaleService saleService)
    {
        _productRepository = productRepository;
        _saleService = saleService;
    }

    /// <summary>One row on the form: a product and a quantity.</summary>
    public class LineInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public record ReceiptLine(string Product, int Quantity, decimal UnitPrice, decimal LineTotal);

    public record Receipt(int SaleId, DateTime SaleDate, string SoldBy,
                          List<ReceiptLine> Lines, decimal Total, long ElapsedMs);

    [BindProperty]
    public List<LineInput> Lines { get; set; } = new();

    /// <summary>Carries the receipt across the redirect, so a refresh can't save the sale twice.</summary>
    [TempData]
    public string? ReceiptJson { get; set; }

    public List<Product> Products { get; private set; } = new();
    public Receipt? LastReceipt { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadProductsAsync();
        Lines = new List<LineInput> { new() };

        if (!string.IsNullOrEmpty(ReceiptJson))
        {
            LastReceipt = JsonSerializer.Deserialize<Receipt>(ReceiptJson);
        }
    }

    public async Task<IActionResult> OnPostAddLineAsync()
    {
        Lines.Add(new LineInput());
        return await ShowFormAsync();
    }

    public async Task<IActionResult> OnPostRemoveLineAsync(int index)
    {
        if (index >= 0 && index < Lines.Count)
        {
            Lines.RemoveAt(index);
        }
        if (Lines.Count == 0)
        {
            Lines.Add(new LineInput());
        }
        return await ShowFormAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await LoadProductsAsync();

        // Rows with no product chosen are ignored.
        var chosen = Lines
            .Where(l => l.ProductId > 0)
            .Select(l => new SaleLine(l.ProductId, l.Quantity))
            .ToList();

        var timer = Stopwatch.StartNew();   // NFR5 evidence (SA08)
        try
        {
            var sale = await _saleService.RecordSaleAsync(chosen, CurrentUserId());
            timer.Stop();

            var names = Products.ToDictionary(p => p.ProductId, p => p.Name);
            var receipt = new Receipt(
                sale.SaleId,
                sale.SaleDate,
                User.Identity?.Name ?? "unknown",
                sale.Items.Select(i => new ReceiptLine(
                    names.GetValueOrDefault(i.ProductId, $"Product {i.ProductId}"),
                    i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
                sale.Total,
                timer.ElapsedMilliseconds);

            ReceiptJson = JsonSerializer.Serialize(receipt);
            return RedirectToPage();
        }
        catch (InsufficientStockException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (InvalidSaleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        // Nothing was saved: show the form again with the user's lines and the message.
        return Page();
    }

    public decimal LineTotal(LineInput line) =>
        Products.FirstOrDefault(p => p.ProductId == line.ProductId) is { } product
            ? product.Price * line.Quantity
            : 0m;

    public decimal RunningTotal => Lines.Sum(l => LineTotal(l));

    public string Rand(decimal amount) =>
        "R" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    private async Task<IActionResult> ShowFormAsync()
    {
        ModelState.Clear();   // so the form shows the updated list, not the posted values
        await LoadProductsAsync();
        return Page();
    }

    private async Task LoadProductsAsync()
    {
        Products = (await _productRepository.GetAllAsync())
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToList();
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}