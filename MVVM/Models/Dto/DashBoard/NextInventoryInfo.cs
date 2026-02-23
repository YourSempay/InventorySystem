using System;

namespace MVVM.Models.Dto.DashBoard;

public class NextInventoryInfo
{
    public DateTime NextInventoryDate { get; set; }
    public int DaysLeft { get; set; }
}