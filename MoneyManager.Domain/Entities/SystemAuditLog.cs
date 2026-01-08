using System;
using System.Collections.Generic;

namespace MoneyManager.Domain.Entities;

public partial class SystemAuditLog
{
    public Guid Id { get; set; }

    public string? ActorId { get; set; }

    public string Action { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? Details { get; set; }

    public DateTime? Timestamp { get; set; }
}
