using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MCHSProject.Services.Auth;
using MCHSProject.DTO.Auth;
using MCHSProject.Validators.Auth;

namespace MCHSProject.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var (isValid, error) = AuthValidator.ValidateRegister(dto);
            if (!isValid)
                return BadRequest(new { message = error });

            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var (isValid, error) = AuthValidator.ValidateLogin(dto);
            if (!isValid)
                return BadRequest(new { message = error });

            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO dto)
        {
            var (isValid, error) = AuthValidator.ValidateRefreshToken(dto);
            if (!isValid)
                return BadRequest(new { message = error });

            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var (isValid, error) = AuthValidator.ValidateChangePassword(dto);
            if (!isValid)
                return BadRequest(new { message = error });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Password changed successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                userId,
                username,
                role
            });
        }
    }
}
