using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MVVM.Services;
using MVVM.ViewModels;

namespace MVVM.Views;

public partial class AccountantMainWindow : Window
{
    public AccountantMainWindow()
    {
        InitializeComponent();
        var vm = new AccountantViewModel(new ApiService(new AuthService()));
        DataContext = vm; 
    }
}