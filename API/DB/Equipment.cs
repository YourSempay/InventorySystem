using System;
using System.Collections.Generic;

namespace API.DB;

public partial class Equipment
{
    public int Id { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Category { get; set; } = null!;

    public string? Status { get; set; }

    public int? AssignedToUserId { get; set; }

    public DateTime? DateAssigned { get; set; }

    public DateTime? LastInventoryDate { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public decimal? Cost { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual ICollection<Assignmenthistory> Assignmenthistories { get; set; } = new List<Assignmenthistory>();

    public virtual ICollection<Inventoryrecord> Inventoryrecords { get; set; } = new List<Inventoryrecord>();
}
