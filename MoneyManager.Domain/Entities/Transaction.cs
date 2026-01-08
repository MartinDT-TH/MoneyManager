using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class Transaction
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public DateTime TransactionDate { get; set; }

    public Guid WalletId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? GroupId { get; set; }

    public string? BillImageUrl { get; set; }

    public string? OcrRawData { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Group? Group { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
