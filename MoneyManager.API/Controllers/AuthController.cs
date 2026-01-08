using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Auth;
using MoneyManager.Application.Interfaces;

namespace MoneyManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Trả về lỗi 400 kèm message từ Service (ví dụ: Email đã tồn tại)
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Trả về lỗi 400 nếu sai pass hoặc tài khoản bị khóa
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}