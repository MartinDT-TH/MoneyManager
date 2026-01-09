using MoneyManager.Application.DTOs;

namespace MoneyManager.Application.Interfaces;

public interface ISpendingForecaster
{
    // Returns the next 7 days of predicted total expenses
    Task<List<DailyForecastDto>> PredictNextWeekExpensesAsync(Guid userId);
}