using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyManager.Domain.Entities;

public partial class Wallet : BaseSyncEntity
{
    public string Name { get; set; } = null!;
    public decimal? Balance { get; set; }
    public string? Currency { get; set; }

    public string Type { get; set; } = null!;

    public Guid OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual AppUser Owner { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
