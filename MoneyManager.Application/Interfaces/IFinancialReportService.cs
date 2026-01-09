using MoneyManager.Application.DTOs;

namespace MoneyManager.Application.Interfaces;

public interface IFinancialReportService
{
    Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(Guid userId, DateTime month);
    Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(Guid userId, int lastMonths);
    // Helper for Internal Use (AI Engine)
    Task<Dictionary<Guid, decimal>> GetAverageSpendingLast3Months(Guid userId);
    Task<decimal> GetTotalIncomeForMonth(Guid userId, DateTime month);
    Task<decimal> GetTotalExpenseForMonth(Guid userId, DateTime month);
}