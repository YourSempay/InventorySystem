using API.DB;
using API.Models.DTO.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[EnableRateLimiting("fixed")]
[Route("api/users")]
[Authorize(Roles = "Accountant")]
public class UsersController : Controller
{
    private readonly  _1135InventorySystemContext db;

    public UsersController(_1135InventorySystemContext db)
    {
        this.db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAllUsers()
    {
        var users = await db.Users.Select(x => new UserResponse
        {
            Id = x.Id,
            Username = x.Username,
            FullName =  x.FullName,
            Email = x.Email,
            Role = x.Role,
            IsActive = x.IsActive.GetValueOrDefault(),
            CreatedAt = x.CreatedAt,
            LastLogin = x.LastLogin
        }).ToListAsync();
        
        return Ok(users);
    }

    [HttpGet("employees")]
    public async Task<ActionResult<List<EmployeeDropdown>>> GetActiveEmployees()
    {
        var employee = await db.Users.Where(x => x.IsActive == true && x.Role == "Employee").Select(x =>
            new EmployeeDropdown
            {
                Id = x.Id,
                FullName = x.FullName,
            }).ToListAsync();
        
        return Ok(employee);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userIdF = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!int.TryParse(userIdF, out var userId))
            return Unauthorized();
        
        var user = await db.Users.Where(x => x.Id == userId).Select(x => new UserResponse
        {
            Id = x.Id,
            Username =  x.Username,
            FullName =  x.FullName,
            Email = x.Email,
            Role = x.Role,
            IsActive = x.IsActive.GetValueOrDefault(),
            CreatedAt =  x.CreatedAt,
            LastLogin = x.LastLogin
        }).FirstOrDefaultAsync();

        if (user == null)
            return NotFound();
        
        return Ok(user);
    }


    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (await db.Users.AnyAsync(x => x.Username == request.Username))
            return Conflict("Пользщователь с таким логином уже существует");        
       
        if (request.Role != "Employee" && request.Role != "Accountant")
            return BadRequest("Недопустимая роль. Доступные: Employee, Accountant.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLogin = null
        };
        
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var responce = new UserResponse()
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive.GetValueOrDefault(),
            CreatedAt =  user.CreatedAt,
            LastLogin = user.LastLogin
        };
        
        return CreatedAtAction(nameof(GetMe), new { id = user.Id }, responce);
    }


    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateUser(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await  db.SaveChangesAsync();
        
        return NoContent();
    }
}