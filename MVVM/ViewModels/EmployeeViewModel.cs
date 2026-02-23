using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using MVVM.Models.Dto.DashBoard;
using MVVM.Models.Dto.Equipment;
using MVVM.Models.Dto.Inventory;
using MVVM.Services;
using MVVM.Tools;
using MVVM.Views;
using AsyncRelayCommand = MVVM.Tools.AsyncRelayCommand;

namespace MVVM.ViewModels;

public class EmployeeViewModel : BaseVM
{
    private readonly ApiService apiService;

    public ObservableCollection<EquipmentShortResponse> MyEquipments { get; } = new();
    public ObservableCollection<NextInventoryInfo> InventoryHistory { get; } = new();

    private EquipmentShortResponse? _selectedEquipment;
    public EquipmentShortResponse? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            SetField(ref _selectedEquipment, value);
            OnPropertyChanged(nameof(CanStartInventory));
        }
    }

    public bool CanStartInventory => SelectedEquipment != null;

    public ObservableCollection<string> InventoryStates { get; } =
        new() { "New", "Good", "RequiresRepair", "Unusable" };

    private string? _selectedInventoryState;
    public string? SelectedInventoryState
    {
        get => _selectedInventoryState;
        set
        {
            SetField(ref _selectedInventoryState, value);
            OnPropertyChanged(nameof(CanSaveInventory));
        }
    }

    private string? _attachedFileName;
    public string? AttachedFileName
    {
        get => _attachedFileName;
        set => SetField(ref _attachedFileName, value);
    }

    public bool CanSaveInventory =>
        SelectedEquipment != null &&
        !string.IsNullOrWhiteSpace(SelectedInventoryState);

    public InventoryCreateRequest CurrentInventory { get; private set; } = new();

    public AsyncRelayCommand LoadMyEquipmentCommand { get; }
    public AsyncRelayCommand LoadDashboardCommand { get; }
    public AsyncRelayCommand StartInventoryCommand { get; }
    public AsyncRelayCommand<Window> AttachPhotoCommand { get; }
    public AsyncRelayCommand SaveInventoryCommand { get; }

    public EmployeeViewModel(ApiService apiService)
    {
        this.apiService = apiService;

        LoadMyEquipmentCommand = new AsyncRelayCommand(LoadMyEquipmentAsync);
        LoadDashboardCommand = new AsyncRelayCommand(LoadDashboardAsync);
        StartInventoryCommand = new AsyncRelayCommand(StartInventoryAsync);
        AttachPhotoCommand = new AsyncRelayCommand<Window>(AttachPhotoAsync);
        SaveInventoryCommand = new AsyncRelayCommand(SaveInventoryAsync);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadMyEquipmentAsync();
        await LoadDashboardAsync();
    }

    private async Task LoadMyEquipmentAsync()
    {
        var list = await apiService.GetAsync<List<EquipmentShortResponse>>("equipment/my");

        MyEquipments.Clear();
        foreach (var eq in list ?? new())
            MyEquipments.Add(eq);
    }

    private async Task LoadDashboardAsync()
    {
        var dashboard = await apiService.GetAsync<EmployeeDashboardResponse>("dashboard/employee");

        InventoryHistory.Clear();
        if (dashboard?.NextInventory != null)
        {
            foreach (var item in dashboard.NextInventory)
                InventoryHistory.Add(item);
        }
    }

    public async Task StartInventoryAsync()
    {
        if (SelectedEquipment == null)
            return;

        ResetForm();

        var window = new InventoryWindow
        {
            DataContext = this
        };

        await window.ShowDialog(
            App.Current.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null
        );

        await LoadMyEquipmentAsync();
        await LoadDashboardAsync();
    }

    public async Task AttachPhotoAsync(Window owner)
    {
        var path = await DialogService.ShowFilePickerAsync(
            owner,
            "Выберите фото",
            new[] { "jpg", "png", "jpeg" });

        if (!string.IsNullOrEmpty(path))
        {
            CurrentInventory.PhotoPath?.Dispose();

            CurrentInventory.PhotoPath = File.OpenRead(path);
            CurrentInventory.FileName = Path.GetFileName(path);
            AttachedFileName = CurrentInventory.FileName;
        }
    }

    private async Task SaveInventoryAsync()
    {
        if (!CanSaveInventory)
            return;

        CurrentInventory.EquipmentId = SelectedEquipment!.Id;
        CurrentInventory.EquipmentCondition = SelectedInventoryState!;

        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(CurrentInventory.EquipmentId.ToString()), "EquipmentId");
        content.Add(new StringContent(CurrentInventory.EquipmentCondition), "EquipmentCondition");
        content.Add(new StringContent(CurrentInventory.IsPresent.ToString()), "IsPresent");
        content.Add(new StringContent(CurrentInventory.Location ?? ""), "Location");
        content.Add(new StringContent(CurrentInventory.Comments ?? ""), "Comments");

        if (CurrentInventory.PhotoPath != null && !string.IsNullOrEmpty(CurrentInventory.FileName))
        {
            CurrentInventory.PhotoPath.Position = 0;

            var streamContent = new StreamContent(CurrentInventory.PhotoPath);
            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            content.Add(streamContent, "PhotoPath", CurrentInventory.FileName);
        }

        await apiService.PostMultipartAsync("inventory", content);

        CurrentInventory.PhotoPath?.Dispose();
        ResetForm();

        await LoadMyEquipmentAsync();
        await LoadDashboardAsync();

        CloseInventoryWindow();
    }

    private void ResetForm()
    {
        SelectedInventoryState = null;

        CurrentInventory.EquipmentId = 0;
        CurrentInventory.EquipmentCondition = null!;
        CurrentInventory.IsPresent = false;
        CurrentInventory.Location = null;
        CurrentInventory.Comments = null;
        CurrentInventory.PhotoPath?.Dispose();
        CurrentInventory.PhotoPath = null;
        CurrentInventory.FileName = null;

        AttachedFileName = null;
    }

    private void CloseInventoryWindow()
    {
        if (App.Current.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows
                .OfType<InventoryWindow>()
                .FirstOrDefault();

            window?.Close();
        }
    }
}