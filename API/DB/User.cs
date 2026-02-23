using System;
using System.Collections.Generic;

namespace API.DB;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public virtual ICollection<Assignmenthistory> AssignmenthistoryAssignedByAccountants { get; set; } = new List<Assignmenthistory>();

    public virtual ICollection<Assignmenthistory> AssignmenthistoryNewUsers { get; set; } = new List<Assignmenthistory>();

    public virtual ICollection<Assignmenthistory> AssignmenthistoryPreviousUsers { get; set; } = new List<Assignmenthistory>();

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<Inventoryrecord> Inventoryrecords { get; set; } = new List<Inventoryrecord>();
}
