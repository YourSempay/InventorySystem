namespace API.Models.DTO.Auth;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int ExpiresIn { get; set; }
}