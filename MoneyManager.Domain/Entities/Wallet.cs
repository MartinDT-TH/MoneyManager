using MoneyManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyManager.Domain.Entities;

public partial class Wallet : BaseSyncEntity
{
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Balance { get; set; }
    public CurrencyCode Currency { get; set; } = CurrencyCode.VND;

    public string Type { get; set; } = null!;

    public Guid OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual AppUser Owner { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
