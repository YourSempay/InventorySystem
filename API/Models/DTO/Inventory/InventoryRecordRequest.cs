namespace API.Models.DTO.Inventory;

public class InventoryRecordRequest
{
    public string EquipmentCondition { get; set; } = null!;
    public bool IsPresent { get; set; }
    public string? Location { get; set; }
    public string? Comments { get; set; }
    public IFormFile? PhotoPath { get; set; }
}