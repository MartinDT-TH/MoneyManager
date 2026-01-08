using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class Budget
{
    public Guid Id { get; set; }

    public decimal AmountLimit { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Guid CategoryId { get; set; }

    public Guid OwnerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual AppUser Owner { get; set; } = null!;
}
