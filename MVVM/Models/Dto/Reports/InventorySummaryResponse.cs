using System;

namespace MVVM.Models.Dto.Reports;

public class InventorySummaryResponse
{
    public int EmployeeId { get; set; }
    public int TotalRecords { get; set; }
    public int MissingCount { get; set; }
    public DateTime? LastInventory { get; set; }
}