using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.DB;
using API.Models.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[ApiController]
[EnableRateLimiting("fixed")]
[Route("api/auth")]
public class AuthController: Controller
{
    private readonly  _1135InventorySystemContext db;
    private readonly IConfiguration configuration;
    private readonly ILogger<AuthController> logger;
    public AuthController(_1135InventorySystemContext db, IConfiguration configuration, ILogger<AuthController> logger)
    {
        this.db = db;
        this.configuration = configuration;
        this.logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive == true);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Неудачная попытка входа: {Username} с IP {IP}", 
                request.Username, HttpContext.Connection.RemoteIpAddress);

            return Unauthorized("Неверный логин или пароль");
        }
        
        user.LastLogin = DateTime.UtcNow;
        await db.SaveChangesAsync();
        
        var token = GenerateJwtToken(user);
        
        return Ok(new LoginResponse()
        {
           Token = token,
           UserId =  user.Id,
           Role = user.Role,
           FullName = user.FullName,
           ExpiresIn = int.Parse(configuration["Jwt:ExpiresInMinutes"]) *  60
               
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return BadRequest("Старый пароль неверный");
            
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            return BadRequest("Новый пароль соотвсествует старому");
    
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();

        return Ok();
    }

    [Authorize]
    [HttpGet("validate")]
    public ActionResult Validate()
    {
        return Ok(new { isValid = true });
    }


    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
        
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(configuration["Jwt:ExpiresInMinutes"]));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
}