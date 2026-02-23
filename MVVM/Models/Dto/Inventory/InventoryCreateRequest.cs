using System.IO;

namespace MVVM.Models.Dto.Inventory;

public class InventoryCreateRequest
{
    public int EquipmentId { get; set; }
    public string EquipmentCondition { get; set; } = null!;
    public bool IsPresent { get; set; }
    public string? Location { get; set; }
    public string? Comments { get; set; }
    
    public Stream? PhotoPath { get; set; }
    public string? FileName { get; set; }
}