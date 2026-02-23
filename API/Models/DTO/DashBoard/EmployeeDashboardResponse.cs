namespace API.Models.DTO.DashBoard;

public class EmployeeDashboardResponse
{
    public int EquipmentCount { get; set; }
    
    public List<NextInventoryInfo> NextInventory { get; set; }
}