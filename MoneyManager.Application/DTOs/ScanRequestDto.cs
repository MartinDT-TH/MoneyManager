using Microsoft.AspNetCore.Http;

namespace MoneyManager.Application.DTOs;

public class ScanRequestDto
{
    public required IFormFile File { get; set; }
    // Sau này có thể thêm:
    // public string? Notes { get; set; }
}