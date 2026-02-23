namespace API.Models.DTO.Reports;

public class InventoryReportDetail
{
    public string InventoryNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EquipmentCondition { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public DateTime InventoryDate { get; set; }
    public string? Location { get; set; }
}