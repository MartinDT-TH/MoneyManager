using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyManager.Domain.Entities;

public partial class Budget : BaseSyncEntity
{
    public decimal AmountLimit { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Guid CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    public Guid OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual AppUser? Owner { get; set; }
}
