namespace API.Models.DTO.Assigments;

public class AssignmentCreateRequest
{
    public int EquipmentId { get; set; }
    public int EmployeeId { get; set; }
    public string? Reason { get; set; }
}