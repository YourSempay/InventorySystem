using System;
using System.Collections.Generic;

namespace API.DB;

public partial class Inventoryrecord
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime InventoryDate { get; set; }

    public string EquipmentCondition { get; set; } = null!;

    public bool IsPresent { get; set; }

    public string? Location { get; set; }

    public string? Comments { get; set; }

    public string? PhotoPath { get; set; }

    public virtual User Employee { get; set; } = null!;

    public virtual Equipment Equipment { get; set; } = null!;
}
