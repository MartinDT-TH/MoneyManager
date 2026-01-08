namespace MoneyManager.Application.DTOs.Auth;

public class AuthResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool IsPremium { get; set; } // Để App hiện icon VIP
    public List<string> Roles { get; set; } = new List<string>();
}