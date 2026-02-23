using System.Security.Claims;
using API.DB;
using API.Models.DTO.DashBoard;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[EnableRateLimiting("fixed")]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : Controller
{
    private readonly _1135InventorySystemContext db;

    private readonly ISystemSettingsService settings;

    public DashboardController(_1135InventorySystemContext db, ISystemSettingsService settings)
    {
        this.db = db;
        this.settings = settings;
    }

    [Authorize(Roles = "Accountant")]
    [HttpGet("accountant")]
    public async Task<ActionResult<AccountantDashboardResponse>> GetAccountantDashboard()
    {
        var totalAvailable = await db.Equipment.CountAsync(x => x.Status == "Available");
        var totalAssigned = await db.Equipment.CountAsync(x => x.Status == "Assigned");
        var underRepair = await db.Equipment.CountAsync(x => x.Status == "UnderRepair");
        var missing = await db.Equipment.CountAsync(x => x.Status == "Missing");

        var overdueDays = await settings.GetSettingValueAsIntAsync("InventoryOverdueDays");
        var overdueInventory = await db.Equipment.Where(x => x.LastInventoryDate == null || x.LastInventoryDate < DateTime.UtcNow.AddDays(-overdueDays)).CountAsync();

        return Ok(new AccountantDashboardResponse
        {
            Available = totalAvailable,
            Assigned = totalAssigned,
            UnderRepair = underRepair,
            Missing = missing,
            OverdueInventory = overdueInventory
        });
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("employee")]
    public async Task<ActionResult<EmployeeDashboardResponse>> GetEmployeeDashboard()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var equipmentCount = await db.Equipment.CountAsync(x => x.AssignedToUserId == userId && x.Status == "Assigned");
        
        var nextReminderDays = await settings.GetSettingValueAsIntAsync("NextInventoryReminderDays");

        var equipments = await db.Equipment.Where(x => x.AssignedToUserId == userId && x.LastInventoryDate != null).ToListAsync();

        var nextInventoryList = equipments.Select(x =>
        {
            var nextDate = x.LastInventoryDate!.Value.AddDays(nextReminderDays);
            var daysLeft = (nextDate.Date - DateTime.UtcNow.Date).Days;

            return new NextInventoryInfo
            {
                NextInventoryDate = nextDate,
                DaysLeft = daysLeft
            };
        }).ToList();

        return Ok(new EmployeeDashboardResponse
        {
            EquipmentCount = equipmentCount,
            NextInventory = nextInventoryList
        });
    }
}