using System.Security.Claims;
using API.DB;
using API.Models.DTO.Assigments;
using API.Models.DTO.Equipment;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AssignmentHistoryResponse = API.Models.DTO.Assigments.AssignmentHistoryResponse;

namespace API.Controllers;

[ApiController]
[EnableRateLimiting("fixed")]
[Route("api/assignments")]
[Authorize]
public class AssignmentsController : Controller
{
    private readonly _1135InventorySystemContext db;
    private readonly ISystemSettingsService settings;

    public AssignmentsController(_1135InventorySystemContext db, ISystemSettingsService settings)
    {
        this.db = db;
        this.settings = settings;
    }

    [Authorize(Roles = "Accountant")]
    [HttpPost]
    public async Task<ActionResult> AssignEquipment([FromBody] AssignmentCreateRequest request)
    {
        var equipment = await db.Equipment.FirstOrDefaultAsync(x => x.Id == request.EquipmentId && x.IsActive != false);
        if (equipment == null)
            return NotFound("Оборудование не найдено");

        if (equipment.Status != "Available")
            return BadRequest("Оборудование недоступно для выдачи");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.IsActive == true);
        if (user == null)
            return NotFound("Сотрудник не найден");

        int limit = await settings.GetSettingValueAsIntAsync("MaxUse");
        int currentAssignments = await db.Equipment.CountAsync(x => x.AssignedToUserId == user.Id && x.Status == "Assigned");
        if (currentAssignments >= limit)
            return BadRequest("Сотрудник достиг лимита выдачи оборудования");

        var assignedBy = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var previousUserId = equipment.AssignedToUserId;
        equipment.AssignedToUserId = user.Id;
        equipment.Status = "Assigned";
        equipment.DateAssigned = DateTime.UtcNow;

        var history = new Assignmenthistory
        {
            EquipmentId = equipment.Id,
            PreviousUserId = previousUserId,
            NewUserId = user.Id,
            AssignedByAccountantId = assignedBy,
            AssignmentDate = DateTime.UtcNow,
            Action = "Assign",
            Reason = request.Reason
        };

        db.Assignmenthistories.Add(history);
        await db.SaveChangesAsync();

        return Ok();
    }
    
    [Authorize(Roles = "Accountant")]
    [HttpPost("{id}/return")]
    public async Task<ActionResult> ReturnEquipment(int id, [FromBody] AssignmentReturnRequest request)
    {
        var equipment = await db.Equipment.FindAsync(id);
        if (equipment == null || equipment.Status != "Assigned")
            return BadRequest("Оборудование не выдано");

        var previousUserId = equipment.AssignedToUserId;
        var assignedBy = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (request.NewEmployeeId.HasValue)
        {
            var newUser = await db.Users.FirstOrDefaultAsync(x => x.Id == request.NewEmployeeId.Value && x.IsActive != false);
            if (newUser == null)
                return NotFound("Новый сотрудник не найден");

            if (newUser.Id == previousUserId)
                return BadRequest("Нельзя передать одному и тому же человеку");

            equipment.AssignedToUserId = newUser.Id;
            equipment.Status = "Assigned";
            equipment.DateAssigned = DateTime.UtcNow;

            var history = new Assignmenthistory
            {
                EquipmentId = equipment.Id,
                PreviousUserId = previousUserId,
                NewUserId = newUser.Id,
                AssignedByAccountantId = assignedBy,
                AssignmentDate = DateTime.UtcNow,
                Action = "Assign",
                Reason = request.Reason
            };

            db.Assignmenthistories.Add(history);
        }
        else
        {
            equipment.AssignedToUserId = null;
            equipment.Status = "Available";
            equipment.DateAssigned = null;

            var history = new Assignmenthistory
            {
                EquipmentId = equipment.Id,
                PreviousUserId = previousUserId,
                NewUserId = assignedBy, 
                AssignedByAccountantId = assignedBy,
                AssignmentDate = DateTime.UtcNow,
                Action = "Return",
                Reason = request.Reason
            };

            db.Assignmenthistories.Add(history);
        }
        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<AssignmentHistoryResponse>>> GetHistory([FromQuery] int? equipmentId = null, [FromQuery] int? employeeId = null)
    {
        var query = db.Assignmenthistories.AsQueryable();

        if (equipmentId.HasValue)
            query = query.Where(x => x.EquipmentId == equipmentId.Value);

        if (employeeId.HasValue)
            query = query.Where(x => x.NewUserId == employeeId.Value || x.PreviousUserId == employeeId.Value);

        var history = await query.Select(x => new AssignmentHistoryResponse
        {
            Id = x.Id,
            PreviousUserId = x.PreviousUserId,
            NewUserId = x.NewUserId,
            AssignedByAccountantId = x.AssignedByAccountantId,
            AssignmentDate = x.AssignmentDate,
            Action = x.Action,
            Reason = x.Reason
        }).ToListAsync();

        return Ok(history);
    }

    [HttpGet("current")]
    public async Task<ActionResult<List<EquipmentShortResponse>>> GetCurrentAssignments()
    {
        var current = await db.Equipment.Where(x => x.Status == "Assigned" && x.IsActive != false)
            .Select(x => new EquipmentShortResponse
            {
                Id = x.Id,
                Name = x.Name,
                Status = x.Status,
                Category = x.Category,
                Cost = x.Cost
            }).ToListAsync();

        return Ok(current);
    }
}