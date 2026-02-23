namespace MVVM.Models.Dto.DashBoard;

public class AccountantDashboardResponse
{
    public int Available { get; set; }
    public int Assigned { get; set; }
    public int UnderRepair { get; set; }
    public int Missing { get; set; }
    public int OverdueInventory { get; set; }
}