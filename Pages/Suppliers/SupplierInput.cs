using System.ComponentModel.DataAnnotations;
using StockSmart.Models;

namespace StockSmart.Pages.Suppliers;

/// <summary>
/// The supplier form, shared by Add and Edit (FR3). The limits match the Supplier table:
/// Name NVARCHAR(100) required, Phone NVARCHAR(20), Email NVARCHAR(100).
/// </summary>
public class SupplierInput
{
    [Required(ErrorMessage = "Enter the supplier's name.")]
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Supplier name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "The phone number can be at most 20 characters.")]
    [RegularExpression(@"^[0-9 +()\-]*$",
        ErrorMessage = "The phone number can only contain digits, spaces, +, - and brackets.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [StringLength(100, ErrorMessage = "The email can be at most 100 characters.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address, for example orders@supplier.co.za.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    /// <summary>Removes stray spaces; an empty phone or email is stored as NULL.</summary>
    public void Tidy()
    {
        Name = Name?.Trim() ?? string.Empty;
        Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
    }

    /// <summary>Turns the validated form into a Supplier.</summary>
    public Supplier ToSupplier(int supplierId = 0) => new()
    {
        SupplierId = supplierId,
        Name = Name,
        Phone = Phone,
        Email = Email
    };

    /// <summary>Fills the form from an existing supplier (used by Edit).</summary>
    public static SupplierInput FromSupplier(Supplier s) => new()
    {
        Name = s.Name,
        Phone = s.Phone,
        Email = s.Email
    };
}