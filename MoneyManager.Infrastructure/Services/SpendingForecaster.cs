using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using MoneyManager.Application.DTOs;
using MoneyManager.Application.Interfaces;
using MoneyManager.Infrastructure.Data.Context;

namespace MoneyManager.Infrastructure.Services;

// Internal classes for ML.NET Pipeline
internal class DailyExpenseData
{
    public DateTime Date { get; set; }
    public float Amount { get; set; }
}

internal class ExpensePrediction
{
    public float[]? ForecastedAmount { get; set; } // Array because we forecast multiple days
}

public class SpendingForecaster(MoneyManagerDbContext _context) : ISpendingForecaster
{
    public async Task<List<DailyForecastDto>> PredictNextWeekExpensesAsync(Guid userId)
    {
        // 1. Fetch Last 90 Days Data
        var startDate = DateTime.UtcNow.AddDays(-90);

        var rawData = await _context.Transactions
            .Where(t => t.Wallet.OwnerId == userId &&
                        t.TransactionDate >= startDate &&
                        t.Amount < 0) // Expenses only
            .GroupBy(t => t.TransactionDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = (float)g.Sum(t => Math.Abs(t.Amount)) // ML.NET prefers float
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // 2. Pre-process: Ensure no gaps in dates (Fill missing days with 0)
        // Time Series SSA requires continuous data.
        var fullData = new List<DailyExpenseData>();
        for (var day = startDate.Date; day < DateTime.UtcNow.Date; day = day.AddDays(1))
        {
            var existing = rawData.FirstOrDefault(x => x.Date == day);
            fullData.Add(new DailyExpenseData
            {
                Date = day,
                Amount = existing?.Total ?? 0f
            });
        }

        // If not enough data points, return empty or simple average
        if (fullData.Count < 10) return [];

        // 3. Setup ML Context
        var mlContext = new MLContext();
        var dataView = mlContext.Data.LoadFromEnumerable(fullData);

        // 4. Build Pipeline: SsaForecasting (Singular Spectrum Analysis)
        var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(ExpensePrediction.ForecastedAmount),
            inputColumnName: nameof(DailyExpenseData.Amount),
            windowSize: 7,       // Analyze patterns in 7-day windows (Weekly cycle)
            seriesLength: 30,    // Total points to look back for training
            trainSize: fullData.Count,
            horizon: 7,          // Predict next 7 days
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: "LowerBound",
            confidenceUpperBoundColumn: "UpperBound");

        // 5. Train Model
        var model = forecastingPipeline.Fit(dataView);

        // 6. Create Prediction Engine
        var forecastingEngine = model.CreateTimeSeriesEngine<DailyExpenseData, ExpensePrediction>(mlContext);

        // 7. Predict
        var forecast = forecastingEngine.Predict();

        // 8. Map to DTO
        var result = new List<DailyForecastDto>();
        var nextDate = DateTime.UtcNow.Date;

        if (forecast.ForecastedAmount != null)
        {
            foreach (var amount in forecast.ForecastedAmount)
            {
                nextDate = nextDate.AddDays(1);
                // Ensure no negative spending predictions
                var predicted = Math.Max(0, amount);
                result.Add(new DailyForecastDto(nextDate, predicted));
            }
        }

        return result;
    }
}