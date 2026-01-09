using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Infrastructure.Data.Context;

namespace MoneyManager.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<MoneyManagerDbContext>();
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

                // 1. Chạy Migration tự động
                await context.Database.MigrateAsync();

                // Kiểm tra nếu đã có dữ liệu Category thì coi như đã seed rồi -> Return
                if (context.Categories.Any()) return;

                // ==================================================
                // 1. SEED ROLES
                // ==================================================
                var roleList = new[]
                {
                    new { Name = "Admin", Description = "Quản trị viên hệ thống, có toàn quyền" },
                    new { Name = "Member", Description = "Người dùng phổ thông, giới hạn quyền theo gói" }
                };

                foreach (var role in roleList)
                {
                    if (!await roleManager.RoleExistsAsync(role.Name))
                    {
                        await roleManager.CreateAsync(new AppRole
                        {
                            Name = role.Name,
                            Description = role.Description // Map description vào đây
                        });
                    }
                }

                // ==================================================
                // 2. SEED USERS
                // ==================================================

                // 2.1 Admin
                var adminUser = new AppUser
                {
                    UserName = "admin@money.com",
                    Email = "admin@money.com",
                    FullName = "System Administrator",
                    IsActive = true,
                    IsPremium = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                if (await userManager.CreateAsync(adminUser, "Admin@123") == IdentityResult.Success)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // 2.2 User Free (Chỉ dùng được 2 ví)
                var freeUser = new AppUser
                {
                    UserName = "free@money.com",
                    Email = "free@money.com",
                    FullName = "Nguyen Van Free",
                    AvatarUrl = "https://i.pravatar.cc/150?u=free",
                    IsActive = true,
                    IsPremium = false,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(freeUser, "User@123");
                await userManager.AddToRoleAsync(freeUser, "Member");

                // 2.3 User Premium (Được tạo nhóm, scan AI)
                var premiumUser = new AppUser
                {
                    UserName = "vip@money.com",
                    Email = "vip@money.com",
                    FullName = "Tran Thi Premium",
                    AvatarUrl = "https://i.pravatar.cc/150?u=vip",
                    IsActive = true,
                    IsPremium = true,
                    PremiumExpiryDate = DateTime.UtcNow.AddYears(1),
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(premiumUser, "User@123");
                await userManager.AddToRoleAsync(premiumUser, "Member");

                // ==================================================
                // 3. SEED CATEGORIES (Danh mục hệ thống - OwnerId = null)
                // ==================================================
                var categories = new List<Category>
                {
                    // EXPENSE
                    new Category { Name = "Ăn uống", Type = CategoryType.Expense, IconCode = "fastfood", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Di chuyển", Type = CategoryType.Expense, IconCode = "commute", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Mua sắm", Type = CategoryType.Expense, IconCode = "shopping_cart", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Giải trí", Type = CategoryType.Expense, IconCode = "movie", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Sức khỏe", Type = CategoryType.Expense, IconCode = "medical_services", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Hóa đơn", Type = CategoryType.Expense, IconCode = "receipt", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Giáo dục", Type = CategoryType.Expense, IconCode = "school", CreatedAt = DateTime.UtcNow },
                    
                    // INCOME
                    new Category { Name = "Lương", Type = CategoryType.Income, IconCode = "payments", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Thưởng", Type = CategoryType.Income, IconCode = "card_giftcard", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Đầu tư", Type = CategoryType.Income, IconCode = "trending_up", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Khác", Type = CategoryType.Income, IconCode = "more_horiz", CreatedAt = DateTime.UtcNow }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync(); // Save để lấy ID Category dùng bên dưới

                // Lấy các list category để dùng random bên dưới
                var expenseCats = categories.Where(c => c.Type == CategoryType.Expense).ToList();
                var incomeCats = categories.Where(c => c.Type == CategoryType.Income).ToList();

                var salaryCat = categories.First(c => c.Name == "Lương");
                var foodCat = categories.First(c => c.Name == "Ăn uống");
                var shoppingCat = categories.First(c => c.Name == "Mua sắm");

                // ==================================================
                // 4. SEED WALLETS
                // ==================================================
                var wallets = new List<Wallet>
                {
                    // User Free: 2 Ví
                    new Wallet { Name = "Tiền mặt", Balance = 2000000, Type = "CASH", OwnerId = freeUser.Id, CreatedAt = DateTime.UtcNow },
                    new Wallet { Name = "Vietcombank", Balance = 15000000, Type = "BANK", OwnerId = freeUser.Id, CreatedAt = DateTime.UtcNow },

                    // User Premium: 3 Ví
                    new Wallet { Name = "Ví Tiêu Dùng", Balance = 5000000, Type = "CASH", OwnerId = premiumUser.Id, CreatedAt = DateTime.UtcNow },
                    new Wallet { Name = "Techcombank", Balance = 50000000, Type = "BANK", OwnerId = premiumUser.Id, CreatedAt = DateTime.UtcNow },
                    new Wallet { Name = "Visa Credit", Balance = -2000000, Type = "CREDIT", OwnerId = premiumUser.Id, CreatedAt = DateTime.UtcNow }
                };
                context.Wallets.AddRange(wallets);
                await context.SaveChangesAsync();

                var freeUserWallet = wallets.First(w => w.OwnerId == freeUser.Id && w.Type == "CASH");
                var premiumUserWallet = wallets.First(w => w.OwnerId == premiumUser.Id && w.Type == "BANK");

                // ==================================================
                // 5. SEED GROUPS & MEMBERS (Quỹ nhóm)
                // ==================================================
                var familyGroup = new Group
                {
                    Name = "Gia đình hạnh phúc",
                    Description = "Quỹ chi tiêu chung",
                    InviteCode = "FAMILY88",
                    CreatedByUserId = premiumUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Groups.Add(familyGroup);
                await context.SaveChangesAsync();

                var groupMembers = new List<GroupMember>
                {
                    // Premium User là Admin nhóm
                    new GroupMember { GroupId = familyGroup.Id, UserId = premiumUser.Id, Role = GroupRole.Admin, JoinedAt = DateTime.UtcNow },
                    // Free User là Member
                    new GroupMember { GroupId = familyGroup.Id, UserId = freeUser.Id, Role = GroupRole.Member, JoinedAt = DateTime.UtcNow }
                };
                context.GroupMembers.AddRange(groupMembers);

                // ==================================================
                // 6. SEED BUDGETS (Ngân sách)
                // ==================================================
                var budget = new Budget
                {
                    AmountLimit = 3000000, // Giới hạn 3tr
                    StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    EndDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1),
                    CategoryId = foodCat.Id,
                    OwnerId = freeUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Budgets.Add(budget);

                // ==================================================
                // 7. SEED TRANSACTIONS (Giao dịch) - Tạo nhiều data cho biểu đồ
                // ==================================================
                var transactions = new List<Transaction>();
                var rnd = new Random();

                // 7.1 Giao dịch cho User Free (30 giao dịch)
                for (int i = 0; i < 30; i++)
                {
                    var isExpense = i % 3 != 0; // 2/3 là chi tiêu
                    // Logic: Income dương, Expense âm (để đồng nhất logic thống kê)
                    var amount = isExpense ? -(rnd.Next(30, 200) * 1000) : (rnd.Next(5000, 10000) * 1000);
                    var date = DateTime.UtcNow.AddDays(-i);// Rải rác từ hôm nay về quá khứ

                    transactions.Add(new Transaction
                    {
                        Amount = amount,
                        Note = isExpense ? $"Mua đồ {i}" : "Nhận lương/thưởng",
                        TransactionDate = date,
                        WalletId = freeUserWallet.Id,
                        CategoryId = isExpense ? (i % 2 == 0 ? foodCat.Id : shoppingCat.Id) : salaryCat.Id,
                        CreatedAt = date,
                        LastUpdatedAt = date
                    });
                }

                // 7.2 Giao dịch Nhóm (User Premium trả tiền ăn cho cả nhóm)
                transactions.Add(new Transaction
                {
                    Amount = -1500000, // Chi tiêu là số âm
                    Note = "Ăn nhà hàng cuối tuần (Quỹ nhóm)",
                    TransactionDate = DateTime.UtcNow.AddDays(-2),
                    WalletId = premiumUserWallet.Id, // Tiền trừ ví Premium
                    CategoryId = foodCat.Id,
                    GroupId = familyGroup.Id, // Gắn vào Group
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                });

                // ====================================================================================
                // 7.3 [NEW] SEED DATA PREMIUM USER - 12 THÁNG GẦN NHẤT
                // ====================================================================================

                // Lặp qua 12 tháng (từ 12 tháng trước đến tháng hiện tại)
                for (int m = 12; m >= 0; m--)
                {
                    var currentMonthDate = DateTime.UtcNow.AddMonths(-m);
                    var year = currentMonthDate.Year;
                    var month = currentMonthDate.Month;
                    var daysInMonth = DateTime.DaysInMonth(year, month);

                    // A. THU NHẬP (Lương cứng hàng tháng) - Ngày mùng 5
                    var salaryDate = new DateTime(year, month, Math.Min(5, daysInMonth), 9, 0, 0);
                    transactions.Add(new Transaction
                    {
                        Amount = 45000000, // Lương 45 triệu
                        Note = $"Lương tháng {month}/{year}",
                        TransactionDate = salaryDate,
                        WalletId = premiumUserWallet.Id,
                        CategoryId = salaryCat.Id,
                        CreatedAt = salaryDate,
                        LastUpdatedAt = salaryDate
                    });

                    // B. THU NHẬP PHỤ (Thỉnh thoảng có thưởng) - Xác suất 30%
                    if (rnd.NextDouble() > 0.7)
                    {
                        var bonusDate = new DateTime(year, month, rnd.Next(15, 25), 10, 0, 0);
                        transactions.Add(new Transaction
                        {
                            Amount = rnd.Next(2000, 10000) * 1000,
                            Note = "Thưởng dự án / Đầu tư",
                            TransactionDate = bonusDate,
                            WalletId = premiumUserWallet.Id,
                            CategoryId = incomeCats.First(c => c.Name == "Thưởng" || c.Name == "Đầu tư").Id,
                            CreatedAt = bonusDate,
                            LastUpdatedAt = bonusDate
                        });
                    }

                    // C. CHI TIÊU HÀNG NGÀY (Random 40 - 60 giao dịch mỗi tháng)
                    int transactionCount = rnd.Next(40, 60);
                    for (int t = 0; t < transactionCount; t++)
                    {
                        // Random ngày giờ
                        var day = rnd.Next(1, daysInMonth + 1);
                        var hour = rnd.Next(7, 23); // Từ 7h sáng đến 11h đêm
                        var minute = rnd.Next(0, 60);
                        var txDate = new DateTime(year, month, day, hour, minute, 0);

                        // Random Category chi tiêu
                        var randomCat = expenseCats[rnd.Next(expenseCats.Count)];

                        // Random Số tiền (Dựa theo loại category cho thực tế)
                        decimal amount = 0;
                        string notePrefix = "";

                        switch (randomCat.Name)
                        {
                            case "Ăn uống":
                                amount = rnd.Next(35, 500) * 1000; // 35k - 500k
                                notePrefix = "Ăn";
                                break;
                            case "Di chuyển":
                                amount = rnd.Next(20, 100) * 1000; // 20k - 100k
                                notePrefix = "Grab/Xăng";
                                break;
                            case "Mua sắm":
                                amount = rnd.Next(200, 3000) * 1000; // 200k - 3tr
                                notePrefix = "Shopee/Siêu thị";
                                break;
                            case "Hóa đơn":
                                // Hóa đơn thường chỉ 1-2 lần, nhưng ở đây random đại
                                amount = rnd.Next(500, 2000) * 1000;
                                notePrefix = "Điện/Nước/Net";
                                break;
                            default:
                                amount = rnd.Next(50, 500) * 1000;
                                notePrefix = "Chi tiêu";
                                break;
                        }

                        transactions.Add(new Transaction
                        {
                            Amount = -amount, // SỐ ÂM CHO CHI TIÊU
                            Note = $"{notePrefix} - {randomCat.Name}",
                            TransactionDate = txDate,
                            WalletId = premiumUserWallet.Id,
                            CategoryId = randomCat.Id,
                            CreatedAt = txDate,
                            LastUpdatedAt = txDate
                        });
                    }
                }

                context.Transactions.AddRange(transactions);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi Seed Data: {ex.Message}");
                throw;
            }
        }
    }
}