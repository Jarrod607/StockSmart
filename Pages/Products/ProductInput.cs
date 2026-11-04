using System.ComponentModel.DataAnnotations;
using StockSmart.Models;

namespace StockSmart.Pages.Products;

/// <summary>
/// The fields on the Add and Edit product forms, with their validation rules (FR2.1).
/// Numbers are nullable so a blank field shows "Enter the..." rather than defaulting to 0.
/// </summary>
public class ProductInput
{
    [Required(ErrorMessage = "Enter the product name.")]
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "The category can be at most 50 characters.")]
    [Display(Name = "Category")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Enter the price.")]
    [Range(0.01, 1000000.0, ErrorMessage = "The price must be more than R0.00.")]
    [Display(Name = "Price (R)")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Enter the quantity in stock.")]
    [Range(0, int.MaxValue, ErrorMessage = "The quantity can't be negative.")]
    [Display(Name = "Quantity in stock")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "Enter the reorder level.")]
    [Range(0, int.MaxValue, ErrorMessage = "The reorder level can't be negative.")]
    [Display(Name = "Reorder level")]
    public int? ReorderLevel { get; set; }

    [Required(ErrorMessage = "Choose a supplier.")]
    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    /// <summary>Removes stray spaces; an empty category is stored as NULL.</summary>
    public void Tidy()
    {
        Name = Name?.Trim() ?? string.Empty;
        Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim();
    }

    /// <summary>Turns the validated form into a Product. Only call after validation passes.</summary>
    public Product ToProduct(int productId = 0) => new()
    {
        ProductId = productId,
        Name = Name,
        Category = Category,
        Price = Price!.Value,
        Quantity = Quantity!.Value,
        ReorderLevel = ReorderLevel!.Value,
        SupplierId = SupplierId!.Value
    };

    /// <summary>Fills the form from an existing product (used by Edit).</summary>
    public static ProductInput FromProduct(Product p) => new()
    {
        Name = p.Name,
        Category = p.Category,
        Price = p.Price,
        Quantity = p.Quantity,
        ReorderLevel = p.ReorderLevel,
        SupplierId = p.SupplierId
    };
}