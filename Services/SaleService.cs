using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Services;

/// <summary>One line the user wants to sell: which product, and how many.</summary>
public record SaleLine(int ProductId, int Quantity);

/// <summary>
/// The business rules for recording a sale (FR4, FR5). Checks the sale, prepares it,
/// then hands it to SaleRepository, which saves it in one transaction (NFR2).
/// </summary>
public class SaleService
{
    private readonly ProductRepository _productRepository;
    private readonly SaleRepository _saleRepository;
    private readonly ILogger<SaleService> _logger;

    public SaleService(ProductRepository productRepository, SaleRepository saleRepository,
                       ILogger<SaleService> logger)
    {
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Records a sale for the given user and reduces stock.
    /// Throws InvalidSaleException or InsufficientStockException if the sale isn't allowed;
    /// in that case nothing is saved.
    /// </summary>
    public async Task<Sale> RecordSaleAsync(IEnumerable<SaleLine> lines, int userId)
    {
        var lineList = lines.ToList();

        // Rule 1: every line needs a quantity of at least 1.
        if (lineList.Any(l => l.Quantity < 1))
        {
            throw new InvalidSaleException("Each line needs a quantity of at least 1.");
        }

        // Rule 2: combine duplicate lines, so the stock check sees the true total per product.
        var combined = lineList
            .GroupBy(l => l.ProductId)
            .Select(g => new SaleLine(g.Key, g.Sum(l => l.Quantity)))
            .ToList();

        if (combined.Count == 0)
        {
            throw new InvalidSaleException("Add at least one product to the sale.");
        }

        var sale = new Sale { SaleDate = DateTime.Now, UserId = userId };

        foreach (var line in combined)
        {
            // Rule 3: only active products can be sold.
            var product = await _productRepository.GetByIdAsync(line.ProductId);
            if (product == null || !product.IsActive)
            {
                throw new InvalidSaleException($"Product {line.ProductId} is not available for sale.");
            }

            // Rule 4: check stock before saving, with a clear message.
            // (SaleRepository checks again inside the transaction, as a safety net.)
            if (line.Quantity > product.Quantity)
            {
                throw new InsufficientStockException(product.ProductId,
                    $"Not enough stock for \"{product.Name}\": {line.Quantity} requested, " +
                    $"only {product.Quantity} available.");
            }

            // Rule 5: copy today's price into the line.
            sale.Items.Add(new SaleItem
            {
                ProductId = product.ProductId,
                Quantity = line.Quantity,
                UnitPrice = product.Price
            });
        }

        // Rule 6: the total is calculated here, never taken from the browser.
        sale.Total = sale.Items.Sum(i => i.LineTotal);

        sale.SaleId = await _saleRepository.CreateSaleAsync(sale);

        _logger.LogInformation("Sale {SaleId} recorded by user {UserId}: {Lines} line(s), total R{Total:0.00}.",
            sale.SaleId, userId, sale.Items.Count, sale.Total);

        return sale;
    }
}