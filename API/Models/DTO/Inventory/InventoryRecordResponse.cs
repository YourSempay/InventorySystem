namespace API.Models.DTO.Inventory;

public class InventoryRecordResponse
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public int EmployeeId { get; set; }
    public string EquipmentCondition { get; set; } = null!;
    public bool IsPresent { get; set; }
    public string? Location { get; set; }
    public string? Comments { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime InventoryDate { get; set; }
}