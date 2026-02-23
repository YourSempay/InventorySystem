using System;
using System.Threading.Tasks;
using MVVM.Models.Dto.Auth;

namespace MVVM.Services;

public class AuthService
{
    public ApiService? apiService { get; set; }
    public string? Token { get; private set; }
    public string? Role { get; private set; }
    public int UserId { get; private set; }
    public string? FullName { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    
    public AuthService()
    {
    }
    
    public AuthService(ApiService apiService)
    {
        this.apiService = apiService;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var response = await apiService.PostAsync<LoginRequest, LoginResponse>("auth/login", new LoginRequest { Username = username, Password = password });
        if (response != null)
        {
            Token = response.Token;
            Role = response.Role;
            UserId = response.UserId;
            FullName = response.FullName;
            return true;
        }

        return false;
    }

    public void Logout()
    {
        Token = null;
        Role = null;
        UserId = 0;
        FullName = null;
    }

    public async Task<bool> ValidateAsync()
    {
        try
        {
            var result = await apiService.GetAsync<object>("auth/validate");
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task ChangePasswordAsync(string oldPassword, string newPassword)
    {
        await apiService.PostAsync("auth/change-password", new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = newPassword
        });
    }
}