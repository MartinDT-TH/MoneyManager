using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class Group
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string InviteCode { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual AppUser CreatedByUser { get; set; } = null!;

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
