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

    // Xử lý cho creadit card nếu cần,  - Ver 2 sẽ dùng những biến này
    // Các cột mới thêm (Bắt buộc phải Nullable - dấu ?)
    //public decimal? CreditLimit { get; set; }
    //public int? StatementDay { get; set; }
    //public int? PaymentDueDay { get; set; }
    //public double? InterestRate { get; set; }

    //// Logic phụ trợ (Helper Property) - Không lưu DB, chỉ để tính toán hiển thị
    //// Số tiền còn được phép tiêu = Hạn mức - Số nợ (Giả sử Balance đang âm)
    //public decimal AvailableBalance => (Type == "CREDIT_CARD" && CreditLimit.HasValue)
    //                                   ? CreditLimit.Value + Balance
    //                                   : Balance;


    public Guid OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual AppUser Owner { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
