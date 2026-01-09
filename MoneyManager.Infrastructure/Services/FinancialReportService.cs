using Microsoft.EntityFrameworkCore;
using MoneyManager.Application.DTOs;
using MoneyManager.Application.Interfaces;
using MoneyManager.Infrastructure.Data.Context;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Infrastructure.Services;

public class FinancialReportService(MoneyManagerDbContext _context) : IFinancialReportService
{
    public async Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(Guid userId, DateTime month)
    {
        var startDate = new DateTime(month.Year, month.Month, 1);
        var endDate = startDate.AddMonths(1);

        // Filter transactions: User -> Date Range -> Expense Type
        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.Wallet.OwnerId == userId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate < endDate &&
                        t.Category.Type ==  CategoryType.Expense);

        var totalExpense = await query.SumAsync(t => Math.Abs(t.Amount));

        if (totalExpense == 0) return [];

        // Group By Category
        var grouped = await query
            .GroupBy(t => t.Category.Name)
            .Select(g => new
            {
                Name = g.Key,
                Total = g.Sum(t => Math.Abs(t.Amount))
            })
            .ToListAsync();

        return grouped.Select(x => new CategoryBreakdownDto(
            x.Name,
            x.Total,
            Math.Round((double)(x.Total / totalExpense) * 100, 2)
        )).OrderByDescending(x => x.TotalAmount).ToList();
    }

    public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(Guid userId, int lastMonths)
    {
        var startDate = DateTime.UtcNow.AddMonths(-lastMonths);

        var data = await _context.Transactions
            .Where(t => t.Wallet.OwnerId == userId && t.TransactionDate >= startDate)
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                // Assuming Amount > 0 is Income, < 0 is Expense
                Income = g.Where(t => t.Amount > 0).Sum(t => t.Amount),
                Expense = g.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount))
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return data.Select(x => new MonthlyTrendDto(
            $"{x.Month}/{x.Year}",
            x.Income,
            x.Expense
        )).ToList();
    }

    // --- Helpers for AI Engine ---

    public async Task<Dictionary<Guid, decimal>> GetAverageSpendingLast3Months(Guid userId)
    {
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        var lastMonthEnd = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1); // Up to start of this month

        // Get total spending per category for the last 3 full months
        var totals = await _context.Transactions
            .Where(t => t.Wallet.OwnerId == userId &&
                        t.TransactionDate >= threeMonthsAgo &&
                        t.TransactionDate < lastMonthEnd &&
                        t.Category.Type == CategoryType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { Id = g.Key, Total = g.Sum(t => Math.Abs(t.Amount)) })
            .ToListAsync();

        // Divide by 3 to get average
        return totals.ToDictionary(k => k.Id, v => v.Total / 3);
    }

    public async Task<decimal> GetTotalIncomeForMonth(Guid userId, DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        return await _context.Transactions
            .Where(t => t.Wallet.OwnerId == userId && t.TransactionDate >= start && t.Amount > 0)
            .SumAsync(t => t.Amount);
    }

    public async Task<decimal> GetTotalExpenseForMonth(Guid userId, DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        return await _context.Transactions
            .Where(t => t.Wallet.OwnerId == userId && t.TransactionDate >= start && t.Amount < 0)
            .SumAsync(t => Math.Abs(t.Amount));
    }
}