using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class GroupMember
{
    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public string? Role { get; set; }

    public DateTime? JoinedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
