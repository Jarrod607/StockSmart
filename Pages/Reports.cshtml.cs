using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StockSmart.Exceptions;
using StockSmart.Models;
using StockSmart.Repositories;

namespace StockSmart.Pages;

/// <summary>
/// Daily and weekly sales reports (FR7). Owner and Manager only:
/// protected by the OwnerOrManager rule in Program.cs (NFR1).
/// </summary>
public class ReportsModel : PageModel
{
    private readonly ReportRepository _reportRepository;
    private readonly ILogger<ReportsModel> _logger;

    public ReportsModel(ReportRepository reportRepository, ILogger<ReportsModel> logger)
    {
        _reportRepository = reportRepository;
        _logger = logger;
    }

    /// <summary>The chosen day. For a weekly report, the week that contains it.</summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? Date { get; set; }

    /// <summary>"day" or "week".</summary>
    [BindProperty(SupportsGet = true)]
    public string? Period { get; set; }

    public bool IsWeek => Period == "week";
    public string RangeLabel { get; private set; } = string.Empty;
    public SalesReport? Report { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        var day = (Date ?? DateTime.Today).Date;
        Date = day;
        Period = Period == "week" ? "week" : "day";

        DateTime from, to;
        if (IsWeek)
        {
            int daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;   // Monday = 0 ... Sunday = 6
            from = day.AddDays(-daysSinceMonday);
            to = from.AddDays(7);
            RangeLabel = $"{from:ddd d MMM} to {to.AddDays(-1):ddd d MMM yyyy}";
        }
        else
        {
            from = day;
            to = day.AddDays(1);
            RangeLabel = day.ToString("dddd d MMMM yyyy");
        }

        try
        {
            Report = await _reportRepository.GetSalesReportAsync(from, to);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Loading the sales report failed.");
            ErrorMessage = "The report could not be loaded. Please try again.";
        }
    }
}