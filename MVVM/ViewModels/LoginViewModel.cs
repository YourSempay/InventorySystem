using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MVVM.Services;
using MVVM.Tools;
using MVVM.Views;
using AsyncRelayCommand = MVVM.Tools.AsyncRelayCommand;

namespace MVVM.ViewModels;

public class LoginViewModel : BaseVM
{
    private readonly AuthService authService;
    private readonly NavigationService navigationService;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    public LoginViewModel(AuthService authService, NavigationService navigationService)
    {
        this.authService = authService;
        this.navigationService = navigationService;

        LoginCommand = new AsyncRelayCommand(LoginAsync);
    }

    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            bool success = await authService.LoginAsync(Username, Password);

            if (success)
            {
                var apiService = new ApiService(authService);

                if (authService.Role == "Accountant")
                {
                    
                    var accountantVm = new AccountantViewModel(apiService);
                  
                    navigationService.NavigateTo<AccountantMainWindow>(accountantVm);
                }
                else
                {
                    var employeeVm = new EmployeeViewModel(apiService);
                    navigationService.NavigateTo<EmployeeMainWindow>(employeeVm);
                }
            }

        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            ErrorMessage = apiEx.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Произошла ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    
}