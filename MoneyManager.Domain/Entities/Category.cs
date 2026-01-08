using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? IconCode { get; set; }

    public string Type { get; set; } = null!;

    public Guid? ParentId { get; set; }

    public Guid? OwnerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual AppUser? Owner { get; set; }

    public virtual Category? Parent { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
