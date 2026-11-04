using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;
using StockSmart.Services;

namespace StockSmart.Pages;

/// <summary>
/// TEMPORARY test page for the sales engine (tests SA01, SA02). Manager only.
/// Replaced by the real New Sale page on Friday, then deleted.
/// </summary>
public class SaleTestModel : PageModel
{
    private const string BookName = "A4 Exercise Book 72 pg";
    private const string BreadName = "White Bread 700 g";
    private const string PenName = "Bic Pen Blue";

    private readonly ProductRepository _productRepository;
    private readonly SaleRepository _saleRepository;
    private readonly SaleService _saleService;

    public SaleTestModel(ProductRepository productRepository, SaleRepository saleRepository,
                         SaleService saleService)
    {
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _saleService = saleService;
    }

    public List<Product> Products { get; private set; } = new();
    public int SaleCount { get; private set; }
    public int SaleItemCount { get; private set; }

    public string? TestName { get; private set; }
    public bool? Passed { get; private set; }
    public List<string> Results { get; } = new();

    private record Snapshot(int Sales, int Items, Dictionary<string, int> Stock);

    public async Task OnGetAsync()
    {
        await TakeSnapshotAsync();
    }

    /// <summary>SA01: a valid sale through SaleService, with a duplicate line to combine.</summary>
    public async Task OnPostValidAsync()
    {
        TestName = "SA01: Valid sale";
        try
        {
            var before = await TakeSnapshotAsync();
            var book = Find(BookName);
            var bread = Find(BreadName);

            var sale = await _saleService.RecordSaleAsync(new[]
            {
                new SaleLine(book.ProductId, 2),
                new SaleLine(bread.ProductId, 1),
                new SaleLine(book.ProductId, 1)     // same product again: should be combined
            }, CurrentUserId());

            var after = await TakeSnapshotAsync();

            Results.Add($"Sale {sale.SaleId} saved: {sale.Items.Count} line(s), total R{sale.Total:0.00}.");
            Results.Add($"{BookName}: {before.Stock[BookName]} → {after.Stock[BookName]} (expected −3).");
            Results.Add($"{BreadName}: {before.Stock[BreadName]} → {after.Stock[BreadName]} (expected −1).");
            Results.Add($"Sale rows: {before.Sales} → {after.Sales} (expected +1).");
            Results.Add($"SaleItem rows: {before.Items} → {after.Items} (expected +2: the book lines were combined).");

            Passed = after.Stock[BookName] == before.Stock[BookName] - 3
                  && after.Stock[BreadName] == before.Stock[BreadName] - 1
                  && after.Sales == before.Sales + 1
                  && after.Items == before.Items + 2;
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    /// <summary>
    /// SA02: rollback. Bypasses SaleService's pre-check and sends the repository a sale
    /// whose first line is fine and whose second line can't be filled.
    /// Line 1 is saved inside the transaction, line 2 fails, and everything must be undone.
    /// </summary>
    public async Task OnPostRollbackAsync()
    {
        TestName = "SA02: Rollback";
        try
        {
            var before = await TakeSnapshotAsync();
            var book = Find(BookName);
            var pen = Find(PenName);

            var sale = new Sale { SaleDate = DateTime.Now, UserId = CurrentUserId() };
            sale.Items.Add(new SaleItem { ProductId = book.ProductId, Quantity = 1, UnitPrice = book.Price });
            sale.Items.Add(new SaleItem { ProductId = pen.ProductId, Quantity = 999, UnitPrice = pen.Price });
            sale.Total = sale.Items.Sum(i => i.LineTotal);

            bool refused = false;
            try
            {
                await _saleRepository.CreateSaleAsync(sale);
            }
            catch (InsufficientStockException ex)
            {
                refused = true;
                Results.Add($"Refused as expected: {ex.Message}");
            }

            var after = await TakeSnapshotAsync();

            Results.Add($"{BookName}: {before.Stock[BookName]} → {after.Stock[BookName]} (expected unchanged, although line 1 had reduced it).");
            Results.Add($"{PenName}: {before.Stock[PenName]} → {after.Stock[PenName]} (expected unchanged).");
            Results.Add($"Sale rows: {before.Sales} → {after.Sales} (expected unchanged).");
            Results.Add($"SaleItem rows: {before.Items} → {after.Items} (expected unchanged).");

            Passed = refused
                  && after.Stock[BookName] == before.Stock[BookName]
                  && after.Stock[PenName] == before.Stock[PenName]
                  && after.Sales == before.Sales
                  && after.Items == before.Items;
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    /// <summary>Service-level stock check: refused before the database is touched.</summary>
    public async Task OnPostOverStockAsync()
    {
        TestName = "Service stock check";
        try
        {
            var before = await TakeSnapshotAsync();
            var pen = Find(PenName);

            bool refused = false;
            try
            {
                await _saleService.RecordSaleAsync(new[] { new SaleLine(pen.ProductId, 999) }, CurrentUserId());
            }
            catch (InsufficientStockException ex)
            {
                refused = true;
                Results.Add($"Refused as expected: {ex.Message}");
            }

            var after = await TakeSnapshotAsync();
            Results.Add($"Sale rows: {before.Sales} → {after.Sales}; SaleItem rows: {before.Items} → {after.Items} (expected unchanged).");

            Passed = refused && after.Sales == before.Sales && after.Items == before.Items;
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private async Task<Snapshot> TakeSnapshotAsync()
    {
        Products = await _productRepository.GetAllAsync();
        var (sales, items) = await _saleRepository.CountAsync();
        SaleCount = sales;
        SaleItemCount = items;
        return new Snapshot(sales, items, Products.ToDictionary(p => p.Name, p => p.Quantity));
    }

    private Product Find(string name) =>
        Products.FirstOrDefault(p => p.Name == name)
        ?? throw new InvalidOperationException($"Test product \"{name}\" was not found or is inactive.");

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void Fail(Exception ex)
    {
        Passed = false;
        Results.Add($"Unexpected error: {ex.GetType().Name}: {ex.Message}");
    }
}