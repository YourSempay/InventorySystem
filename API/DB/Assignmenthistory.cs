using System;
using System.Collections.Generic;

namespace API.DB;

public partial class Assignmenthistory
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    public int? PreviousUserId { get; set; }

    public int NewUserId { get; set; }

    public int AssignedByAccountantId { get; set; }

    public DateTime? AssignmentDate { get; set; }

    public string Action { get; set; } = null!;

    public string? Reason { get; set; }

    public virtual User AssignedByAccountant { get; set; } = null!;

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual User NewUser { get; set; } = null!;

    public virtual User? PreviousUser { get; set; }
}
