namespace API.Models.DTO.Reports;

public class AssignmentHistoryReportResponse
{
    public int Id { get; set; }
    public string EquipmentName { get; set; } = null!;
    public string Action { get; set; } = null!;
    public int? PreviousUserId { get; set; }
    public string NewUser { get; set; } = null!;
    public string Accountant { get; set; } = null!;
    public DateTime? AssignmentDate { get; set; }
    public string? Reason { get; set; }
}