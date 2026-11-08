using System.Data;
using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Models;

namespace StockSmart.Repositories;

/// <summary>
/// Reads sales figures for the daily and weekly reports (FR7).
/// Read-only, and all SQL is parameterised (NFR1).
/// </summary>
public class ReportRepository : RepositoryBase
{
    public ReportRepository(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>Builds the report for sales from 'from' (included) up to 'to' (not included).</summary>
    public async Task<SalesReport> GetSalesReportAsync(DateTime from, DateTime to)
    {
        var report = new SalesReport { From = from, To = to };

        try
        {
            using var connection = await OpenConnectionAsync();

            // 1. Number of sales and their total value.
            using (var command = CreateCommand(connection,
                @"SELECT COUNT(*), ISNULL(SUM(Total), 0)
                  FROM dbo.Sale
                  WHERE SaleDate >= @from AND SaleDate < @to;", from, to))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    report.SaleCount = Convert.ToInt32(reader[0]);
                    report.TotalSales = Convert.ToDecimal(reader[1]);
                }
            }

            // 2. Number of items sold.
            using (var command = CreateCommand(connection,
                @"SELECT ISNULL(SUM(si.Quantity), 0)
                  FROM dbo.SaleItem si
                  JOIN dbo.Sale s ON s.SaleId = si.SaleId
                  WHERE s.SaleDate >= @from AND s.SaleDate < @to;", from, to))
            {
                report.ItemsSold = Convert.ToInt32(await command.ExecuteScalarAsync());
            }

            // 3. Top 5 best sellers by quantity (ties: higher revenue first, then name).
            using (var command = CreateCommand(connection,
                @"SELECT TOP 5 p.Name, SUM(si.Quantity) AS QuantitySold,
                         SUM(si.Quantity * si.UnitPrice) AS Revenue
                  FROM dbo.SaleItem si
                  JOIN dbo.Sale s ON s.SaleId = si.SaleId
                  JOIN dbo.Product p ON p.ProductId = si.ProductId
                  WHERE s.SaleDate >= @from AND s.SaleDate < @to
                  GROUP BY p.ProductId, p.Name
                  ORDER BY QuantitySold DESC, Revenue DESC, p.Name;", from, to))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    report.BestSellers.Add(new BestSeller
                    {
                        ProductName = reader.GetString(0),
                        QuantitySold = Convert.ToInt32(reader[1]),
                        Revenue = Convert.ToDecimal(reader[2])
                    });
                }
            }

            // 4. One total per day (used by the weekly report).
            using (var command = CreateCommand(connection,
                @"SELECT CAST(SaleDate AS date) AS Day, COUNT(*), SUM(Total)
                  FROM dbo.Sale
                  WHERE SaleDate >= @from AND SaleDate < @to
                  GROUP BY CAST(SaleDate AS date)
                  ORDER BY Day;", from, to))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    report.DailyTotals.Add(new DailyTotal
                    {
                        Day = reader.GetDateTime(0),
                        SaleCount = Convert.ToInt32(reader[1]),
                        Total = Convert.ToDecimal(reader[2])
                    });
                }
            }
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("Could not load the sales report.", ex);
        }

        return report;
    }

    /// <summary>Creates a command with the two date parameters every report query uses.</summary>
    private static SqlCommand CreateCommand(SqlConnection connection, string sql, DateTime from, DateTime to)
    {
        var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@from", SqlDbType.DateTime2).Value = from;
        command.Parameters.Add("@to", SqlDbType.DateTime2).Value = to;
        return command;
    }
}