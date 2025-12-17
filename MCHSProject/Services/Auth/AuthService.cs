using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;
using MCHSProject.DTO.Auth;
using MCHSProject.Common.Exceptions;

namespace MCHSProject.Services.Auth
{
    public class AuthService
    {
        private readonly DBConnect _dbConnect;
        private readonly IConfiguration _configuration;

        public AuthService(DBConnect dbConnect, IConfiguration configuration)
        {
            _dbConnect = dbConnect;
            _configuration = configuration;
        }

        public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto)
        {
            using var connection = _dbConnect.CreateConnection();

            var existingUser = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE username = @Username", 
                new { dto.Username });

            if (existingUser != null)
                throw new ConflictException("Username already exists");

            var passwordHash = HashPassword(dto.Password);

            var sql = @"INSERT INTO users (username, password_hash, role_id, created_at) 
                       VALUES (@Username, @PasswordHash, 1, @CreatedAt) 
                       RETURNING id";

            var userId = await connection.ExecuteScalarAsync<int>(sql, new
            {
                dto.Username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            });

            var role = await connection.QueryFirstOrDefaultAsync<Role>(
                "SELECT * FROM roles WHERE id = 1");

            return await GenerateAuthResponseAsync(userId, dto.Username, role?.Name ?? "User");
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO dto)
        {
            using var connection = _dbConnect.CreateConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE username = @Username",
                new { dto.Username });

            if (user == null)
                throw new UnauthorizedException("Invalid username or password");

            if (!VerifyPassword(dto.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid username or password");

            var role = await connection.QueryFirstOrDefaultAsync<Role>(
                "SELECT * FROM roles WHERE id = @RoleId",
                new { user.RoleId });

            return await GenerateAuthResponseAsync(user.Id, user.Username, role?.Name ?? "User");
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(string refreshToken)
        {
            using var connection = _dbConnect.CreateConnection();

            var storedToken = await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                "SELECT * FROM refresh_tokens WHERE token = @Token AND is_revoked = false AND expires_at > @Now",
                new { Token = refreshToken, Now = DateTime.UtcNow });

            if (storedToken == null)
                throw new UnauthorizedException("Invalid or expired refresh token");

            await connection.ExecuteAsync(
                "UPDATE refresh_tokens SET is_revoked = true WHERE id = @Id",
                new { storedToken.Id });

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE id = @UserId",
                new { storedToken.UserId });

            if (user == null)
                throw new UnauthorizedException("User not found");

            var role = await connection.QueryFirstOrDefaultAsync<Role>(
                "SELECT * FROM roles WHERE id = @RoleId",
                new { user.RoleId });

            return await GenerateAuthResponseAsync(user.Id, user.Username, role?.Name ?? "User");
        }

        public async Task LogoutAsync(int userId)
        {
            using var connection = _dbConnect.CreateConnection();
            await connection.ExecuteAsync(
                "UPDATE refresh_tokens SET is_revoked = true WHERE user_id = @UserId",
                new { UserId = userId });
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDTO dto)
        {
            using var connection = _dbConnect.CreateConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE id = @UserId",
                new { UserId = userId });

            if (user == null)
                throw new NotFoundException("User not found");

            if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new BadRequestException("Current password is incorrect");

            var newPasswordHash = HashPassword(dto.NewPassword);

            await connection.ExecuteAsync(
                "UPDATE users SET password_hash = @PasswordHash WHERE id = @UserId",
                new { PasswordHash = newPasswordHash, UserId = userId });

            await connection.ExecuteAsync(
                "UPDATE refresh_tokens SET is_revoked = true WHERE user_id = @UserId",
                new { UserId = userId });
        }

        private async Task<AuthResponseDTO> GenerateAuthResponseAsync(int userId, string username, string role)
        {
            var accessToken = GenerateAccessToken(userId, username, role);
            var refreshToken = GenerateRefreshToken();
            var accessTokenExpires = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"));

            using var connection = _dbConnect.CreateConnection();
            await connection.ExecuteAsync(
                @"INSERT INTO refresh_tokens (user_id, token, expires_at, created_at, is_revoked) 
                  VALUES (@UserId, @Token, @ExpiresAt, @CreatedAt, false)",
                new
                {
                    UserId = userId,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(
                        int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
                    CreatedAt = DateTime.UtcNow
                });

            return new AuthResponseDTO
            {
                UserId = userId,
                Username = username,
                Role = role,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpires = accessTokenExpires
            };
        }

        private string GenerateAccessToken(int userId, string username, string role)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? throw new Exception("JWT Secret not configured")));
            
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
