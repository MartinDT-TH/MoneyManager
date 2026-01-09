using Microsoft.AspNetCore.Http;
using MoneyManager.Application.DTOs;

namespace MoneyManager.Application.Interfaces;

public interface IBillScanningService
{
    Task<BillScanResult> ScanBillAsync(IFormFile imageFile);
}