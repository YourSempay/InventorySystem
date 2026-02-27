using System.Collections.Generic;

namespace MVVM.Models.Dto.Reports;

public class InventoryReportResponse
{
    public string EmployeeName { get; set; } = string.Empty;
    public int TotalEquipment { get; set; }
    public int InventoriedCount { get; set; }
    public int MissingCount { get; set; }
    public List<InventoryReportDetail> Details { get; set; } = new List<InventoryReportDetail>();
}