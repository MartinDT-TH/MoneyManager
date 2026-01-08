using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class Wallet
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal? Balance { get; set; }

    public string? Currency { get; set; }

    public string Type { get; set; } = null!;

    public Guid OwnerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual AppUser Owner { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
