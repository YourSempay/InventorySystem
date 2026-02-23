using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
        Reason = string.Empty; // очистка
        SelectedEmployee = null;
        SelectedEquipment = null;
        await LoadAssignments();
    }
}