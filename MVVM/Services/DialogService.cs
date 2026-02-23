using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MVVM.Services;

public class DialogService
{
    private readonly Window owner;

    public DialogService(Window owner)
    {
        this.owner = owner;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Content = new TextBlock { Text = message, Margin = new Thickness(20) },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        await dialog.ShowDialog(owner);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = message });

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        var yesBtn = new Button { Content = "Да", Margin = new Thickness(5) };
        var noBtn = new Button { Content = "Нет", Margin = new Thickness(5) };

        yesBtn.Click += (_, _) => { tcs.SetResult(true); dialog.Close(); };
        noBtn.Click += (_, _) => { tcs.SetResult(false); dialog.Close(); };

        buttons.Children.Add(yesBtn);
        buttons.Children.Add(noBtn);
        panel.Children.Add(buttons);

        dialog.Content = panel;
        await dialog.ShowDialog(owner);

        return await tcs.Task;
    }

    public static async Task<string?> ShowFilePickerAsync(Window owner, string title, string[] filters)
    {
        var dlg = new OpenFileDialog
        {
            Title = title,
            AllowMultiple = false,
            Filters = filters.Select(f => new FileDialogFilter { Name = f, Extensions = new List<string> { f } }).ToList()
        };

        var result = await dlg.ShowAsync(owner);
        return result?.FirstOrDefault();
    }
}