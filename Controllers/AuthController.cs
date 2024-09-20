using BCrypt.Net;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TestPOSApp.Data;
using TestPOSApp.Models;

namespace TestPOSApp.Controllers
{
    [EnableCors("AllowCors")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AuthController(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        #region register
        [HttpPost("register")]
        public async Task<ActionResult<ResponseDto>> Register([FromBody] AuthDt dt)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_context.Users.Any(u => u.Username == dt.Username))
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Username already exists"
                });
            }

            var user = new User
            {
                Username = dt.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dt.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "User registered successfully"
            });
        }
        #endregion

        #region login
        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthDt dt)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == dt.Username);
            if (user == null || !VerifyPassword(dt.Password, user.PasswordHash))
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid username or password"
                });
            }

            var token = GenerateJwtToken(user);
            var loginDto = new LoginDto
            {
                Token = token
            };

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Login successful",
                Data = loginDto
            });
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        #endregion
    }
}
