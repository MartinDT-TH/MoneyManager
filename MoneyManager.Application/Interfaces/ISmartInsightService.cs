using MoneyManager.Application.DTOs;

namespace MoneyManager.Application.Interfaces;

public interface ISmartInsightService
{
    //Task<List<InsightItem>> GenerateInsightsAsync(Guid userId);
    Task<AnalysisReportDto> GenerateAnalysisReportAsync(Guid userId); 
}