namespace API.Models.DTO.Assigments;

public class AssignmentHistoryResponse
{
    public int Id { get; set; }
    public int? PreviousUserId { get; set; }
    public int NewUserId { get; set; }
    public int AssignedByAccountantId { get; set; }
    public DateTime? AssignmentDate { get; set; }
    public string Action { get; set; } = null!;
    public string? Reason { get; set; }
}