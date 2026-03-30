using AuthService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly string _jwtSecret;

        public AuthController(IMongoClient mongoClient, IConfiguration config)
        {
            var database = mongoClient.GetDatabase("AuthServiceDb");
            _usersCollection = database.GetCollection<User>("Users");
            _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                         ?? "BiletSistemi-JWT-Gizli-Anahtar-2026-SuperSecret!";
        }

        /// <summary>
        /// Kullanıcı girişi - JWT token döndürür
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, request.Username);
            var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { error = "Geçersiz kullanıcı adı veya şifre ağam!" });

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                username = user.Username,
                role = user.Role,
                expiresIn = "24 saat"
            });
        }

        /// <summary>
        /// Token doğrulama endpoint'i
        /// </summary>
        [HttpPost("validate")]
        public IActionResult Validate([FromBody] TokenRequest request)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);
                tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "BiletSistemi",
                    ValidateAudience = true,
                    ValidAudience = "BiletSistemi",
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return Ok(new { valid = true, message = "Token geçerli!" });
            }
            catch
            {
                return Unauthorized(new { valid = false, message = "Token geçersiz veya süresi dolmuş!" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("userId", user.Id ?? string.Empty),
            };

            var token = new JwtSecurityToken(
                issuer: "BiletSistemi",
                audience: "BiletSistemi",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public record TokenRequest(string Token);
}
