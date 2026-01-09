namespace MoneyManager.Application.DTOs;

// Task 1: DTOs for Flutter Charts
public record CategoryBreakdownDto(string CategoryName, decimal TotalAmount, double Percentage);

public record MonthlyTrendDto(string MonthLabel, decimal TotalIncome, decimal TotalExpense);

// Task 2: DTOs for Insights
public enum InsightType { Info, Warning, Critical, Praise }

public record InsightItem(
    InsightType Type,
    string Title,
    string Message,
    string? RelatedCategoryName = null
);

// Task 3: DTOs for Forecasting
public record DailyForecastDto(DateTime Date, float PredictedAmount);

// Task 4: DTO for Analysis Report
public record AnalysisReportDto(
    string AnalysisText,        // For the "ANALYSIS" section
    string RecommendationText,  // For the "RECOMMENDATION" section
    List<InsightItem> Details   // The specific warnings we calculated before
);