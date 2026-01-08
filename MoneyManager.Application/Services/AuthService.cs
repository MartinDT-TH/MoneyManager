using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MoneyManager.Application.DTOs.Auth;
using MoneyManager.Application.Interfaces;
using MoneyManager.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MoneyManager.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    // CHỈ Inject UserManager và IConfiguration. KHÔNG dùng SignInManager.
    public AuthService(UserManager<AppUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. Check Email tồn tại
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new Exception("Email này đã được sử dụng.");

        // 2. Tạo User mới
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true,       // Mặc định Active
            IsPremium = false,     // Mặc định Free
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception(errors);
        }

        // 3. Gán quyền Member
        await _userManager.AddToRoleAsync(user, "Member");

        // 4. Trả về Token luôn
        return await GenerateJwtToken(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // 1. Tìm User
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new Exception("Tài khoản hoặc mật khẩu không đúng.");

        // 2. Check Password (Dùng UserManager thay vì SignInManager)
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new Exception("Tài khoản hoặc mật khẩu không đúng.");

        // 3. Check Logic nghiệp vụ: Bị Ban thì không cho vào
        if (!user.IsActive)
            throw new Exception($"Tài khoản đã bị khóa. Lý do: {user.BanReason}");

        // 4. Sinh Token
        return await GenerateJwtToken(user);
    }

    private async Task<AuthResponse> GenerateJwtToken(AppUser user)
    {
        var authClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim("FullName", user.FullName ?? ""), // Custom Claim
            new Claim("IsPremium", user.IsPremium.ToString()), // Custom Claim
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddDays(30), // Token sống 30 ngày cho Mobile App
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new AuthResponse
        {
            Id = user.Id.ToString(),
            Email = user.Email!,
            FullName = user.FullName!,
            IsPremium = user.IsPremium,
            Roles = userRoles.ToList(),
            Token = new JwtSecurityTokenHandler().WriteToken(token)
        };
    }
}