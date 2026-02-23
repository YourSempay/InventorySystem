namespace API.Models.DTO.Reports;

public class MissingEquipmentResponse
{
    public int EquipmentId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime InventoryDate { get; set; }
    public string? Location { get; set; }
    public string? Comments { get; set; }
}