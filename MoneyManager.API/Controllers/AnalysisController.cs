using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Interfaces;
using System.Security.Claims; // For finding UserId

namespace MoneyManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize] // Assume User must be logged in
public class AnalysisController(
    IFinancialReportService reportService,
    ISmartInsightService insightService,
    ISpendingForecaster forecaster) : ControllerBase
{
    //private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid UserId => Guid.Parse("B3C8F1F3-8B5B-46D0-F737-08DE4F53860F");

    [HttpGet("breakdown")]
    public async Task<IActionResult> GetSpendingBreakdown([FromQuery] DateTime? month)
    {
        var targetMonth = month ?? DateTime.UtcNow;
        var result = await reportService.GetCategoryBreakdownAsync(UserId, targetMonth);
        return Ok(result);
    }

    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend()
    {
        var result = await reportService.GetMonthlyTrendAsync(UserId, 6); // Last 6 months
        return Ok(result);
    }

    [HttpGet("insights")]
    public async Task<IActionResult> GetSmartInsights()
    {
        var result = await insightService.GenerateAnalysisReportAsync(UserId);

        // Kết quả trả về bây giờ sẽ là JSON có format:
        // {
        //    "analysisText": "...",
        //    "recommendationText": "...",
        //    "details": [...]
        // }
        return Ok(result);
    }

    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast()
    {
        try
        {
            var result = await forecaster.PredictNextWeekExpensesAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // ML.NET might fail if not enough data
            return BadRequest(new { Message = "Not enough data to generate forecast.", Details = ex.Message });
        }
    }
}