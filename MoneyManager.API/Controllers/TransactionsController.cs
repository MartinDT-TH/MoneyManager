using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Interfaces;

namespace MoneyManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(
    IBillScanningService billScanningService,
    ICategorizationService predictionService) : ControllerBase
{
    // Dùng IFormFile trực tiếp để tránh lỗi Swagger
    [HttpPost("scan-bill")]
    public async Task<IActionResult> ScanBill(IFormFile image)
    {
        // 1. Validate đầu vào
        if (image is null || image.Length == 0)
            return BadRequest("Vui lòng tải lên ảnh hóa đơn.");

        try
        {
            //// Bước 1: Gọi Service OCR (Đã tích hợp sẵn logic quét Google Vision)
            //// Lưu ý: Trong kiến trúc cũ, Service này ĐÃ gọi ML.NET rồi.
            //// Nhưng nếu bạn muốn tách ra để kiểm soát ở Controller như ý bạn muốn:
            //var billInfo = await billScanningService.ScanBillAsync(image);

            //// Bước 2: Gọi ML.NET đoán danh mục (Nếu Service chưa làm bước này hoặc bạn muốn override)
            //// Nếu tên quán tìm được, thì đoán. Nếu không, để mặc định.
            //string finalCategory = billInfo.SuggestedCategory ?? "Chi tiêu khác";

            //if (string.IsNullOrEmpty(billInfo.SuggestedCategory) && !string.IsNullOrEmpty(billInfo.VendorName))
            //{
            //    finalCategory = predictionService.PredictCategory(billInfo.VendorName);
            //}

            //// Bước 3: Trả về JSON chuẩn format cho Flutter
            //return Ok(new
            //{
            //    Amount = billInfo.TotalAmount ?? 0, // Nếu null thì về 0
            //    Date = billInfo.TransactionDate ?? DateTime.Now,
            //    Note = billInfo.VendorName,         // Lấy tên quán làm Note
            //    SuggestedCategory = finalCategory,
            //    RawData = billInfo.RawText          // Text thô để debug (nếu có)
            //});



            // 1. GỌI OCR SERVICE: Chỉ để lấy chữ, tiền, ngày
            var ocrResult = await billScanningService.ScanBillAsync(image);

            // 2. GỌI AI SERVICE: Đoán danh mục dựa trên tên quán (VendorName)
            string predictedCategory = "Chi tiêu khác"; // Giá trị mặc định

            if (!string.IsNullOrEmpty(ocrResult.VendorName))
            {
                var cleanVendor = NormalizeVendorName(ocrResult.VendorName);
                predictedCategory = predictionService.PredictCategory(cleanVendor);
            }

            // 3. TỔNG HỢP KẾT QUẢ TRẢ VỀ FLUTTER
            return Ok(new
            {
                Amount = ocrResult.TotalAmount ?? 0,
                Date = ocrResult.TransactionDate ?? DateTime.Now,
                Note = ocrResult.VendorName,        // Gợi ý tên quán làm ghi chú
                SuggestedCategory = predictedCategory, // Kết quả từ AI
                RawData = ocrResult.RawText         // Text gốc để debug
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // Hàm chuẩn hóa tên quán trước khi đoán
    private string NormalizeVendorName(string rawVendor)
    {
        if (string.IsNullOrEmpty(rawVendor)) return "";

        // 1. Chuyển về chữ thường
        var clean = rawVendor.ToLower().Trim();

        // 2. Xóa địa chỉ chi nhánh (Thường tên quán hay kèm theo địa chỉ dài ngoằng)
        // Ví dụ: "Highlands Coffee - 123 Nguyen Hue" -> cắt bỏ phần sau dấu gạch
        if (clean.Contains("-")) clean = clean.Split('-')[0].Trim();
        if (clean.Contains("cn")) clean = clean.Split("cn")[0].Trim(); // CN = Chi nhánh

        return clean;
        // Kết quả: "highlands coffee" -> Dễ đoán hơn nhiều
    }
}