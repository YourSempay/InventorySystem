using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MVVM.Services;
using MVVM.ViewModels;

namespace MVVM.Views;

public partial class EmployeeMainWindow : Window
{
    public EmployeeMainWindow()
    {
        InitializeComponent();
        DataContext = new EmployeeViewModel(new ApiService(new  AuthService()));
    }
}