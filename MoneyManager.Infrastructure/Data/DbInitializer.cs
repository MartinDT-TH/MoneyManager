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
                    
                    // INCOME
                    new Category { Name = "Lương", Type = CategoryType.Income, IconCode = "payments", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Thưởng", Type = CategoryType.Income, IconCode = "card_giftcard", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Đầu tư", Type = CategoryType.Income, IconCode = "trending_up", CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Khác", Type = CategoryType.Income, IconCode = "more_horiz", CreatedAt = DateTime.UtcNow }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync(); // Save để lấy ID Category dùng bên dưới

                // Lấy ID category để dùng cho Transaction
                var foodCat = categories.First(c => c.Name == "Ăn uống");
                var salaryCat = categories.First(c => c.Name == "Lương");
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

                // 7.1 Giao dịch cho User Free (30 giao dịch)
                // Tạo data cho tháng này và tháng trước
                for (int i = 0; i < 30; i++)
                {
                    var isExpense = i % 3 != 0; // 2/3 là chi tiêu
                    var amount = isExpense ? new Random().Next(30, 200) * 1000 : new Random().Next(5000, 10000) * 1000;
                    var date = DateTime.UtcNow.AddDays(-i); // Rải rác từ hôm nay về quá khứ

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
                    Amount = 1500000,
                    Note = "Ăn nhà hàng cuối tuần (Quỹ nhóm)",
                    TransactionDate = DateTime.UtcNow.AddDays(-2),
                    WalletId = premiumUserWallet.Id, // Tiền trừ ví Premium
                    CategoryId = foodCat.Id,
                    GroupId = familyGroup.Id, // Gắn vào Group
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                });

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