using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace MVVM.Services;

public class NavigationService
{
    private readonly IClassicDesktopStyleApplicationLifetime desktop;

    public NavigationService(IClassicDesktopStyleApplicationLifetime desktop)
    {
        this.desktop = desktop;
    }

    public void NavigateTo<TWindow>(object? dataContext = null) where TWindow : Window, new()
    {
        var newWindow = new TWindow();

        if (dataContext != null)
            newWindow.DataContext = dataContext;

        var oldWindow = desktop.MainWindow;

        desktop.MainWindow = newWindow;

        newWindow.Show();

        oldWindow?.Close();
    }

    public void Close(Window window)
    {
        window.Close();
    }
}