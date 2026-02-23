namespace API.Models.DTO.Equipment;

public class EquipmentCreateRequest
{
    public string InventoryNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public decimal? Cost { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateTime? LastInventoryDate { get; set; }

}