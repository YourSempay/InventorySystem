using System.Collections.Generic;

namespace MVVM.Models.Dto.DashBoard;

public class EmployeeDashboardResponse
{
    public int EquipmentCount { get; set; }
    
    public List<NextInventoryInfo> NextInventory { get; set; }
}