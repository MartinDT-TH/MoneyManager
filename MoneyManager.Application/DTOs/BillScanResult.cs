namespace MoneyManager.Application.DTOs;

// usage of record for immutable DTOs
public record BillScanResult(
    decimal? TotalAmount,
    DateTime? TransactionDate,
    string? VendorName,
    //string? SuggestedCategory,
    string? RawText
);