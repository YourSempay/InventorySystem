namespace API.Models.DTO.Equipment;

public class EquipmentShortResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Status { get; set; }
    public string Category { get; set; } = null!;
    public decimal? Cost { get; set; }
}