using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class SubscriptionLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ProductId { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public string? StoreTransactionId { get; set; }

    public string? OriginalTransactionId { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public string Status { get; set; } = null!;

    public DateTime PurchaseDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
