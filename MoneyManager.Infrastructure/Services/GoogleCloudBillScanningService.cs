using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Http;
using MoneyManager.Application.DTOs;
using MoneyManager.Application.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MoneyManager.Infrastructure.Services;

public class GoogleCloudBillScanningService : IBillScanningService
{
    public async Task<BillScanResult> ScanBillAsync(IFormFile imageFile)
    {
        if (imageFile.Length == 0) throw new ArgumentException("File is empty");

        using var stream = new MemoryStream();
        await imageFile.CopyToAsync(stream);
        stream.Position = 0;

        var image = Image.FromStream(stream);
        var client = await ImageAnnotatorClient.CreateAsync();
        var response = await client.DetectTextAsync(image);

        if (response == null || response.Count == 0)
            return new BillScanResult(null, null, null, "No text detected");

        var fullText = response[0].Description;
        var lines = fullText.Split('\n');

        // Logic xử lý mới theo từng dòng
        var vendor = ParseVendor(lines);
        var amount = ParseTotalAmount(lines); // Truyền vào mảng dòng thay vì text gộp
        var date = ParseDate(fullText);

        return new BillScanResult(amount, date, vendor, fullText);
    }

    // --- 1. LOGIC TÌM TIỀN (NÂNG CẤP MẠNH) ---
    private static decimal? ParseTotalAmount(string[] lines)
    {
        // Ưu tiên 1: Tìm dòng chứa từ khóa "Tổng/Total" và số tiền nằm NGAY ĐÓ hoặc DÒNG KẾ TIẾP
        // Case FamilyMart: "TỔNG CỘNG:" (dòng i) -> "3.250.000 VNĐ" (dòng i+1)
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (Regex.IsMatch(line, @"(?:Tổng\s*(?:cộng|tiền)|Thành\s*tiền|Thanh\s*toán|Total|Amount)", RegexOptions.IgnoreCase))
            {
                // Tìm trong dòng hiện tại
                var amountCurrent = ExtractMoneyFromText(line);
                if (amountCurrent.HasValue) return amountCurrent;

                // Nếu không có, tìm ở dòng kế tiếp (thường là số tiền nằm dưới chữ Tổng)
                if (i + 1 < lines.Length)
                {
                    var amountNext = ExtractMoneyFromText(lines[i + 1]);
                    if (amountNext.HasValue) return amountNext;
                }
            }
        }

        // Ưu tiên 2: Tìm dòng có định dạng tiền tệ rõ ràng (100.000 VND, VND 20,000)
        // Case Techcombank/MB: "VND 20,000" hoặc "20,000 VND"
        foreach (var line in lines)
        {
            // Regex bắt buộc phải có chữ d/đ/VND đi kèm số để tránh nhầm với giờ/ngày/số tài khoản
            // Chấp nhận: "50,000 VND", "VND 50,000", "50.000đ"
            if (Regex.IsMatch(line, @"(\d+[.,]\d+.*(VND|đ|VNĐ))|((VND|VNĐ).* \d+[.,]\d+)", RegexOptions.IgnoreCase))
            {
                var amount = ExtractMoneyFromText(line);
                if (amount.HasValue) return amount;
            }
        }

        // Ưu tiên 3 (Cuối cùng): Tìm số lớn nhất có định dạng tiền tệ (xxx.xxx) trong toàn văn bản
        return FindLargestNumber(lines);
    }

    private static decimal? ExtractMoneyFromText(string text)
    {
        // Regex tìm chuỗi số: 100.000, 1,000,000 hoặc 50000
        var matches = Regex.Matches(text, @"[\d,.]+");
        foreach (Match match in matches)
        {
            var raw = match.Value;
            // Bỏ qua nếu giống giờ (20:04 -> 20.04) hoặc ngày (20.09.2025)
            if (raw.Contains(":") || raw.Count(c => c == '.') >= 2 || raw.Count(c => c == '/') >= 2) continue;

            // Chuẩn hóa: 100.000 -> 100000, 20,000 -> 20000
            // Logic: Nếu có cả dấu chấm và phẩy, dấu nào nằm sau cùng thì là phân cách thập phân (USD), 
            // nhưng ở VN thường là số nguyên. Ta replace hết cho an toàn với tiền Việt.
            var clean = raw.Replace(".", "").Replace(",", "").Replace(" ", "");

            if (decimal.TryParse(clean, out var val))
            {
                // Lọc nhiễu: Tiền thường > 1000 đồng
                if (val > 1000) return val;
            }
        }
        return null;
    }

    private static decimal? FindLargestNumber(string[] lines)
    {
        decimal max = 0;
        foreach (var line in lines)
        {
            var val = ExtractMoneyFromText(line);
            if (val.HasValue && val > max && val < 10_000_000_000) // < 10 tỷ
                max = val.Value;
        }
        return max > 0 ? max : null;
    }

    // --- 2. LOGIC TÌM NGÀY (THÊM FORMAT TIẾNG VIỆT) ---
    private static DateTime? ParseDate(string text)
    {
        // Case Techcombank: "20 thg 9, 2025"
        var vietnamesePattern = @"(\d{1,2})\s*(?:thg|tháng)\s*(\d{1,2})[,\s]*(\d{4})";
        var vnMatch = Regex.Match(text, vietnamesePattern, RegexOptions.IgnoreCase);
        if (vnMatch.Success)
        {
            var day = int.Parse(vnMatch.Groups[1].Value);
            var month = int.Parse(vnMatch.Groups[2].Value);
            var year = int.Parse(vnMatch.Groups[3].Value);
            return new DateTime(year, month, day);
        }

        // Case chuẩn: 20:04 26/12/2025
        var dateTimePattern = @"(\d{1,2}[:]\d{2}).*?(\d{1,2}[/-]\d{1,2}[/-]\d{4})";
        var match = Regex.Match(text, dateTimePattern, RegexOptions.Singleline);
        if (match.Success)
        {
            var dateStr = match.Groups[2].Value.Replace("-", "/") + " " + match.Groups[1].Value;
            if (DateTime.TryParseExact(dateStr, "d/M/yyyy H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;
        }

        // Fallback: dd/MM/yyyy
        var dateOnlyMatch = Regex.Match(text, @"\b(\d{1,2}[/-]\d{1,2}[/-]\d{4})\b");
        if (dateOnlyMatch.Success)
        {
            if (DateTime.TryParseExact(dateOnlyMatch.Value.Replace("-", "/"), "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
        }

        return null;
    }

    // --- 3. LOGIC TÌM VENDOR (LỌC NHIỄU MẠNH HƠN) ---
    private static string? ParseVendor(string[] lines)
    {
        foreach (var line in lines)
        {
            var text = line.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Bỏ dòng rác hệ thống
            if (Regex.IsMatch(text, @"^\d{1,2}[:.]\d{2}", RegexOptions.IgnoreCase)) continue; // Giờ 09:41
            if (Regex.IsMatch(text, @"^\d{1,3}%?$")) continue; // Pin 41%
            if (text.Length < 3) continue; // Bỏ chữ quá ngắn

            var lower = text.ToLower();
            // Bỏ dòng chứa "Mobile", "Bank" dạng thông báo hệ thống (TPBank Mobile)
            // NHƯNG giữ lại nếu nó đứng 1 mình là tên bank (VD: "Techcombank")
            if (lower.Contains("mobile") || lower == "now" || lower == "bây giờ") continue;

            // Bỏ dòng tin nhắn SMS notification (VD: (TPBank): 26/12...)
            if (text.StartsWith("(") && text.Contains("):")) continue;

            // Bỏ các nhãn
            if (lower.StartsWith("lời nhắn") || lower.StartsWith("mã giao dịch") || lower.Contains("số dư")) continue;

            // Dòng còn lại khả năng cao là Vendor hoặc nội dung chính
            return text;
        }
        return null;
    }
}