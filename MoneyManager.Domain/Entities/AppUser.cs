using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyManager.Domain.Entities
{
    public class AppUser : IdentityUser<Guid> // Id là Guid
    {
        public string? FullName { get; set; }

        public string? AvatarUrl { get; set; }

        public bool IsPremium { get; set; } = false;

        public DateTime? PremiumExpiryDate { get; set; }

        public bool IsActive { get; set; } = false;

        public string? BanReason { get; set; }

        public DateTime? CreatedAt { get; set; }

        // --- Navigation Properties (Quan hệ bảng) ---

        public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

        public virtual ICollection<SubscriptionLog> SubscriptionLogs { get; set; } = new List<SubscriptionLog>();

        public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
    }
    public class AppRole : IdentityRole<Guid> { }
}