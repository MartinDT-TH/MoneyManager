using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyManager.Domain.Entities;

public class SubscriptionLog : BaseSystemEntity
{
    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual AppUser? User { get; set; }

    public string ProductId { get; set; } = string.Empty;
    public string? StoreTransactionId { get; set; }
    public string? OriginalTransactionId { get; set; }
    public string Platform { get; set; } = "ANDROID"; // ANDROID, IOS
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "SUCCESS";
    public DateTime PurchaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }
}