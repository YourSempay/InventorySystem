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