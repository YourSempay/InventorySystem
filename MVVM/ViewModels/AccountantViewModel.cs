using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MVVM.Models.Dto.Assigments;
using MVVM.Models.Dto.DashBoard;
using MVVM.Models.Dto.Equipment;
using MVVM.Models.Dto.Reports;
using MVVM.Models.Dto.Users;
using MVVM.Services;
using MVVM.Tools;

namespace MVVM.ViewModels;

public class AccountantViewModel : BaseVM
{
    private readonly ApiService apiService;


    public int Available
    {
        get => _available;
        set => SetField(ref _available, value);
    }

    private int _available;

    public int Assigned
    {
        get => _assigned;
        set => SetField(ref _assigned, value);
    }

    private int _assigned;

    public int UnderRepair
    {
        get => _underRepair;
        set => SetField(ref _underRepair, value);
    }

    private int _underRepair;

    public int Missing
    {
        get => _missing;
        set => SetField(ref _missing, value);
    }

    private int _missing;

    public int OverdueInventory
    {
        get => _overdue;
        set => SetField(ref _overdue, value);
    }

    private int _overdue;

    private string _reason = string.Empty;

    public string Reason
    {
        get => _reason;
        set => SetField(ref _reason, value);
    }


    private DateTimeOffset _reportStartDate = DateTimeOffset.UtcNow.AddMonths(-1);
    public DateTimeOffset ReportStartDate
    {
        get => _reportStartDate;
        set => SetField(ref _reportStartDate, value);
    }

    private DateTimeOffset _reportEndDate = DateTimeOffset.UtcNow;
    public DateTimeOffset ReportEndDate
    {
        get => _reportEndDate;
        set => SetField(ref _reportEndDate, value);
    }

    private EmployeeDropdown? _selectedReportEmployee;
    public EmployeeDropdown? SelectedReportEmployee
    {
        get => _selectedReportEmployee;
        set
        {
            if (SetField(ref _selectedReportEmployee, value))
            {
                FilterSummary();       // обновляем DataGrid при выборе сотрудника
                UpdateExportEnabled(); // обновляем состояние кнопки Export
            }
        }
    }
    
    private string _selectedReportType = "PDF";
    public string SelectedReportType
    {
        get => _selectedReportType;
        set => SetField(ref _selectedReportType, value);
    }
    
    private bool _canExport;
    public bool CanExport
    {
        get => _canExport;
        set => SetField(ref _canExport, value);
    }

    private void UpdateExportEnabled()
    {
        CanExport = SelectedReportEmployee != null && SelectedReportEmployee.Id != -1;
    }

    private void FilterSummary()
    {
        if (SelectedReportEmployee == null || SelectedReportEmployee.Id == -1)
        {
            _filteredSummary.Clear();
            foreach (var item in Summary)
                _filteredSummary.Add(item);
        }
        else
        {
            var filtered = Summary.Where(s => s.EmployeeId == SelectedReportEmployee.Id);
            _filteredSummary.Clear();
            foreach (var item in filtered)
                _filteredSummary.Add(item);
        }
    }

    private ObservableCollection<InventorySummaryResponse> _filteredSummary = new();
    public ObservableCollection<InventorySummaryResponse> FilteredSummary => _filteredSummary;

    public AsyncRelayCommand ExportCommand { get; }
    
    public ObservableCollection<DashboardItem> DashboardItems { get; } = new();
    public ObservableCollection<EquipmentShortResponse> Equipments { get; } = new();
    public ObservableCollection<EmployeeDropdown> Employees { get; } = new();
    public ObservableCollection<EquipmentShortResponse> AvailableEquipment { get; } = new();

    private EmployeeDropdown? _selectedEmployee;

    public EmployeeDropdown? SelectedEmployee
    {
        get => _selectedEmployee;
        set => SetField(ref _selectedEmployee, value);
    }

    private EquipmentShortResponse? _selectedEquipment;

    public EquipmentShortResponse? SelectedEquipment
    {
        get => _selectedEquipment;
        set => SetField(ref _selectedEquipment, value);
    }
    


    public ObservableCollection<InventorySummaryResponse> Summary { get; } = new();

    public AsyncRelayCommand AssignCommand { get; }

    public AccountantViewModel(ApiService apiService)
    {
        this.apiService = apiService;
        AssignCommand = new AsyncRelayCommand(AssignAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        _ = LoadAll();

    }

    private async Task LoadAll()
    {
        await LoadDashboard();
        await LoadEquipment();
        await LoadAssignments();
        await LoadReports();
    }

    private async Task LoadDashboard()
    {
        var response = await apiService.GetAsync<AccountantDashboardResponse>("dashboard/accountant");
        if (response != null)
        {
            Available = response.Available;
            Assigned = response.Assigned;
            UnderRepair = response.UnderRepair;
            Missing = response.Missing;
            OverdueInventory = response.OverdueInventory;
            
            
            DashboardItems.Clear();
            DashboardItems.Add(new DashboardItem { Label = "Доступно", Value = Available, Background="LightGreen", Foreground="#4B3F2F" });
            DashboardItems.Add(new DashboardItem { Label = "Выдано", Value = Assigned, Background="LightBlue", Foreground="#4B3F2F" });
            DashboardItems.Add(new DashboardItem { Label = "На ремонте", Value = UnderRepair, Background="Orange", Foreground="#4B3F2F" });
            DashboardItems.Add(new DashboardItem { Label = "Отсутствует", Value = Missing, Background="Red", Foreground="#4B3F2F" });
            DashboardItems.Add(new DashboardItem { Label = "Просрочено", Value = OverdueInventory, Background="Gray", Foreground="#4B3F2F" });
        }
    }

    private async Task LoadEquipment()
    {
        var list = await apiService.GetAsync<List<EquipmentShortResponse>>("equipment");
        foreach (var e in list)
            Console.WriteLine($"Id={e.Id}, Name={e.Name}, Category={e.Category}");
        if (list != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Equipments.Clear();
                foreach (var item in list)
                    Equipments.Add(item);
            });
        }
    }

    private async Task LoadAssignments()
    {
        var employees = await apiService.GetAsync<List<EmployeeDropdown>>("users/employees");
        if (employees != null)
        {
            Employees.Clear();
            Employees.Add(new EmployeeDropdown { Id = -1, FullName = "All" });
            foreach (var e in employees) Employees.Add(e);
        }

        var equipment = await apiService.GetAsync<List<EquipmentShortResponse>>("equipment/available");
        if (equipment != null)
        {
            AvailableEquipment.Clear();
            foreach (var eq in equipment) AvailableEquipment.Add(eq);
        }
    }

    private async Task LoadReports()
    {
        var list = await apiService.GetAsync<List<InventorySummaryResponse>>("reports/inventory-summary");
        if (list != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Summary.Clear();
                foreach (var item in list)
                    Summary.Add(item);
            });
        }
    }

    private async Task AssignAsync()
    {
        if (SelectedEmployee == null || SelectedEquipment == null || string.IsNullOrWhiteSpace(Reason)) 
            return;

        var request = new AssignmentCreateRequest
        {
            EmployeeId = SelectedEmployee.Id,
            EquipmentId = SelectedEquipment.Id,
            Reason = Reason
        };

        await apiService.PostAsync("assignments", request);
        Reason = string.Empty; 
        SelectedEmployee = null;
        SelectedEquipment = null;
        await LoadAssignments();
    }
    
    private async Task ExportAsync()
    {
        var request = new ExportReportRequest
        {
            EmployeeId = SelectedReportEmployee?.Id,
            StartDate = ReportStartDate.DateTime,
            EndDate = ReportEndDate.DateTime,
            Type = SelectedReportType
        };

        var fileBytes = await apiService.PostForFileAsync("reports/export", request);
        if (fileBytes == null)
            return;

        var extension = SelectedReportType == "PDF" ? "pdf" : "xlsx";
        var fileName = $"InventoryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}";

        var saveDialog = new SaveFileDialog
        {
            InitialFileName = fileName
        };

        var mainWindow =
            (Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        var path = await saveDialog.ShowAsync(mainWindow);

        if (!string.IsNullOrEmpty(path))
        {
            await File.WriteAllBytesAsync(path, fileBytes);

            // 🚀 Авто-открытие файла
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}
public class DashboardItem
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
    public string Background { get; set; } = "#FFF8F0";
    public string Foreground { get; set; } = "#4B3F2F";
}