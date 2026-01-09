using Microsoft.EntityFrameworkCore;
using MoneyManager.Application.DTOs;
using MoneyManager.Application.Interfaces;
using MoneyManager.Domain.Enums;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Data.Context;
using System.Globalization;
using System.Text;

namespace MoneyManager.Infrastructure.Services;

public class SmartInsightService(
    MoneyManagerDbContext _context,
    IFinancialReportService _reportService) : ISmartInsightService
{
    public async Task<AnalysisReportDto> GenerateAnalysisReportAsync(Guid userId)
    {
        var insights = new List<InsightItem>();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // --- CẤU HÌNH ĐỊNH DẠNG TIỀN VIỆT NAM ---
        var viVn = CultureInfo.GetCultureInfo("vi-VN");

        // =================================================================================
        // 1. LẤY DỮ LIỆU (TUẦN TỰ)
        // =================================================================================

        // 1.1 Lịch sử & Chi tiêu hiện tại
        var history = await _reportService.GetAverageSpendingLast3Months(userId);

        var current = await _context.Transactions
            .Where(t => t.Wallet.OwnerId == userId
                        && t.TransactionDate >= startOfMonth
                        && t.Amount < 0)
            .Include(t => t.Category)
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                Total = g.Sum(t => Math.Abs(t.Amount))
            })
            .ToListAsync();

        // 1.2 Ngân sách
        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.Category.OwnerId == userId)
            .ToListAsync();

        // 1.3 Tổng quan
        var income = await _reportService.GetTotalIncomeForMonth(userId, now);
        var totalExpense = await _reportService.GetTotalExpenseForMonth(userId, now);

        // =================================================================================
        // 2. TẠO CÁC CẢNH BÁO CHI TIẾT (VIỆT HÓA)
        // =================================================================================

        // Rule 1: Chi tiêu bất thường
        foreach (var category in current)
        {
            if (history.TryGetValue(category.CategoryId, out decimal avg) && avg > 0)
            {
                if (category.Total > avg * 1.2m)
                {
                    var percent = (int)((category.Total - avg) / avg * 100);
                    insights.Add(new InsightItem(
                        InsightType.Warning,
                        "Chi tiêu bất thường",
                        $"Bạn đã tiêu cho {category.Name} nhiều hơn {percent}% so với trung bình 3 tháng qua.",
                        category.Name));
                }
            }
        }

        // Rule 2: Ngân sách
        foreach (var budget in budgets)
        {
            var spent = current.FirstOrDefault(c => c.CategoryId == budget.CategoryId)?.Total ?? 0;
            if (spent >= budget.AmountLimit)
            {
                insights.Add(new InsightItem(
                    InsightType.Critical,
                    "Vỡ ngân sách",
                    $"Bạn đã tiêu vượt quá hạn mức cho danh mục {budget.Category?.Name}.",
                    budget.Category?.Name));
            }
        }

        // =================================================================================
        // 3. VIẾT BÁO CÁO TỰ ĐỘNG (NARRATIVE GENERATION - VIETNAMESE)
        // =================================================================================

        // --- 3.1 Phần "PHÂN TÍCH" (ANALYSIS) ---
        var balance = income - totalExpense;
        var analysisBuilder = new StringBuilder();

        // Format tiền tệ dùng biến viVn để ra dạng 100.000 đ
        analysisBuilder.Append($"Biểu đồ cho thấy tổng thu nhập là {income.ToString("C0", viVn)} và tổng chi tiêu là {totalExpense.ToString("C0", viVn)}, ");

        if (balance >= 0)
        {
            analysisBuilder.Append($"dẫn đến khoản dư {balance.ToString("C0", viVn)}. ");
            analysisBuilder.Append("Bạn đang duy trì cân bằng tài chính rất tốt trong tháng này. ");
        }
        else
        {
            analysisBuilder.Append($"dẫn đến thâm hụt {Math.Abs(balance).ToString("C0", viVn)}. ");
            analysisBuilder.Append("Xu hướng này cho thấy chi tiêu đang tăng nhanh hơn thu nhập. ");
        }

        // Kiểm tra độ biến động (Nếu tổng chi lớn hơn 10% so với trung bình quá khứ)
        decimal avgTotalExpense = history.Sum(x => x.Value);
        if (avgTotalExpense > 0 && totalExpense > avgTotalExpense * 1.1m)
        {
            analysisBuilder.Append("Chi tiêu đang tăng cao so với mức trung bình lịch sử của bạn, cho thấy xu hướng tiêu dùng mạnh về cuối kỳ.");
        }

        // --- 3.2 Phần "KHUYẾN NGHỊ" (RECOMMENDATION) ---
        var recommendationBuilder = new StringBuilder();

        if (balance < 0)
        {
            // Trường hợp thâm hụt
            recommendationBuilder.Append("Bạn nên cố gắng giới hạn chi tiêu ở mức 80-85% tổng thu nhập. ");
            recommendationBuilder.Append("Việc theo dõi và phân loại chi tiêu thường xuyên là rất quan trọng để nhận diện các khoản lãng phí. ");

            // Tìm danh mục tiêu nhiều nhất để đưa lời khuyên cụ thể
            var topCategory = current.OrderByDescending(c => c.Total).FirstOrDefault();
            if (topCategory != null)
            {
                recommendationBuilder.Append($"Hãy cân nhắc cắt giảm chi tiêu trong mục '{topCategory.Name}' vì đây là khoản tốn kém nhất tháng này. ");
            }
        }
        else if (income > 0 && totalExpense < income * 0.5m)
        {
            // Trường hợp tiết kiệm tốt (>50% thu nhập)
            recommendationBuilder.Append("Làm tốt lắm! Bạn đã tiết kiệm được một phần lớn thu nhập. ");
            recommendationBuilder.Append("Hãy cân nhắc đầu tư khoản dư này vào quỹ dự phòng (3-6 tháng sinh hoạt phí) hoặc các tài sản sinh lời.");
        }
        else
        {
            // Trường hợp bình thường
            recommendationBuilder.Append("Hãy duy trì thói quen chi tiêu hiện tại. ");
            recommendationBuilder.Append("Sử dụng các công cụ quản lý tài chính tự động sẽ giúp bạn theo dõi xu hướng và đưa ra quyết định thông minh hơn.");
        }

        return new AnalysisReportDto(
            AnalysisText: analysisBuilder.ToString(),
            RecommendationText: recommendationBuilder.ToString(),
            Details: insights
        );
    }


    //public async Task<List<InsightItem>> GenerateInsightsAsync(Guid userId)
    //{
    //    var insights = new List<InsightItem>();
    //    var now = DateTime.UtcNow;
    //    var startOfMonth = new DateTime(now.Year, now.Month, 1);

    //    // =================================================================================
    //    // 1. DATA FETCHING (SỬA LẠI: CHẠY TUẦN TỰ ĐỂ TRÁNH LỖI DB CONTEXT THREADING)
    //    // =================================================================================

    //    // Lưu ý: EF Core DbContext không hỗ trợ đa luồng (multi-thread) trên cùng 1 request.
    //    // Phải dùng await từng cái một, hoặc tạo scope riêng (phức tạp hơn).
    //    // Ở đây ta dùng await tuần tự cho an toàn và đơn giản.

    //    // 1.1 Lấy lịch sử chi tiêu trung bình 3 tháng (Gọi qua Service)
    //    var history = await _reportService.GetAverageSpendingLast3Months(userId);

    //    // 1.2 Lấy chi tiêu tháng hiện tại (Query trực tiếp DB)
    //    // Gom nhóm theo Category ngay tại DB để tối ưu hiệu năng
    //    var current = await _context.Transactions
    //        .Where(t => t.Wallet.OwnerId == userId
    //                    && t.TransactionDate >= startOfMonth
    //                    //&& t.Category.Type == CategoryType.Expense // Đảm bảo Enum/String khớp với Domain của bạn
    //                    && t.Amount < 0) // Hoặc check theo Amount < 0 nếu chưa có Enum
    //        .Include(t => t.Category)
    //        .GroupBy(t => new { t.CategoryId, t.Category.Name })
    //        .Select(g => new
    //        {
    //            g.Key.CategoryId,
    //            g.Key.Name,
    //            Total = g.Sum(t => Math.Abs(t.Amount))
    //        })
    //        .ToListAsync();

    //    // 1.3 Lấy danh sách ngân sách (Budgets)
    //    var budgets = await _context.Budgets
    //        .Include(b => b.Category) // Include để lấy tên Category hiển thị
    //        .Where(b => b.Category.OwnerId == userId)
    //        .ToListAsync();

    //    // 1.4 Lấy tổng thu/chi tháng này
    //    var income = await _reportService.GetTotalIncomeForMonth(userId, now);
    //    var totalSpent = await _reportService.GetTotalExpenseForMonth(userId, now);

    //    // (Đã xóa Task.WhenAll để tránh crash DBContext)

    //    // =================================================================================
    //    // 2. ANALYZE & GENERATE INSIGHTS
    //    // =================================================================================

    //    // --- RULE 1: Anomaly Detection (Phát hiện chi tiêu bất thường) ---
    //    foreach (var category in current)
    //    {
    //        // So sánh với trung bình 3 tháng trước
    //        if (history.TryGetValue(category.CategoryId, out decimal avg) && avg > 0)
    //        {
    //            // Ngưỡng cảnh báo: Nếu cao hơn 20% so với trung bình (avg * 1.2)
    //            if (category.Total > avg * 1.2m)
    //            {
    //                var percent = (int)((category.Total - avg) / avg * 100);
    //                insights.Add(new InsightItem(
    //                    InsightType.Warning,
    //                    "Cảnh báo chi tiêu lạ", // Tiêu đề
    //                    $"Bạn đã tiêu cho {category.Name} nhiều hơn {percent}% so với trung bình 3 tháng qua.",
    //                    category.Name
    //                ));
    //            }
    //        }
    //    }

    //    // --- RULE 2: Budget Health (Sức khỏe ngân sách) ---
    //    foreach (var budget in budgets)
    //    {
    //        // Tìm số tiền đã chi cho danh mục này trong tháng hiện tại
    //        var spent = current.FirstOrDefault(c => c.CategoryId == budget.CategoryId)?.Total ?? 0;
    //        var catName = budget.Category?.Name ?? "Danh mục";

    //        // Cảnh báo nhẹ: Đã dùng 80% ngân sách
    //        if (spent >= budget.AmountLimit * 0.8m && spent < budget.AmountLimit)
    //        {
    //            var percentUsed = (int)(spent / budget.AmountLimit * 100);
    //            insights.Add(new InsightItem(
    //                InsightType.Warning,
    //                "Sắp hết ngân sách",
    //                $"Bạn đã dùng {percentUsed}% ngân sách cho {catName}.",
    //                catName
    //            ));
    //        }
    //        // Cảnh báo đỏ: Đã vỡ ngân sách
    //        else if (spent >= budget.AmountLimit)
    //        {
    //            insights.Add(new InsightItem(
    //                InsightType.Critical,
    //                "Vỡ ngân sách!",
    //                $"Bạn đã chi vượt quá hạn mức cho {catName}.",
    //                catName
    //            ));
    //        }
    //    }

    //    // --- RULE 3: Forecasting (Dự báo cháy túi - Burn Rate) ---
    //    // Logic: Chỉ chạy kiểm tra này vào giữa tháng (từ ngày 10 đến 20)
    //    if (now.Day is >= 10 and <= 20 && income > 0)
    //    {
    //        var burnRate = totalSpent / income;

    //        // Nếu mới ngày 15 mà đã tiêu hết 70% lương -> Nguy hiểm
    //        if (burnRate > 0.7m && now.Day <= 15)
    //        {
    //            // Tính toán tuyến tính đơn giản: Tốc độ tiêu tiền trung bình/ngày
    //            var dailyRate = totalSpent / now.Day;
    //            if (dailyRate > 0)
    //            {
    //                var estimatedDayZero = income / dailyRate; // Ngày dự kiến hết tiền

    //                // Nếu ngày hết tiền < số ngày trong tháng -> Sẽ bị âm tiền
    //                if (estimatedDayZero < DateTime.DaysInMonth(now.Year, now.Month))
    //                {
    //                    insights.Add(new InsightItem(
    //                        InsightType.Critical,
    //                        "Cảnh báo dòng tiền",
    //                        $"Với tốc độ tiêu này, bạn có thể sẽ tiêu hết thu nhập vào ngày {(int)estimatedDayZero}."
    //                    ));
    //                }
    //            }
    //        }
    //    }

    //    return insights;
    //}
}