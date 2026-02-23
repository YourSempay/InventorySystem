using System.Security.Claims;
using API.DB;
using API.Models.DTO.Assigments;
using API.Models.DTO.Equipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[EnableRateLimiting("fixed")]
[Route("api/equipment")]
public class EquipmentController: Controller
{
    private readonly  _1135InventorySystemContext db;

    public EquipmentController(_1135InventorySystemContext db)
    {
        this.db = db;        
    }

    [Authorize(Roles = "Accountant")]
    [HttpGet]
    public async Task<ActionResult<List<EquipmentResponse>>> GetAllEquipments([FromQuery] int page = 1, [FromQuery]  int pageSize = 20, [FromQuery] string? status = null)
    {
        var query = db.Equipment.Where(x => x.IsActive != false);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);
        
        var result = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new EquipmentShortResponse
            {
                Id = x.Id,
                Name = x.Name,
                Category = x.Category,
                Cost = x.Cost,
                Status = x.Status
            }).ToListAsync();

        return Ok(result);
    }

    [Authorize(Roles = "Accountant")]
    [HttpGet("available")]
    public async Task<ActionResult<List<EquipmentResponse>>> GetAvailable()
    {
        var result = await db.Equipment.Where(x=> x.Status == "Available" && x.IsActive != false)
            .Select(x => new EquipmentShortResponse
            {
                Id = x.Id,
                Name = x.Name,
                Category = x.Category,
                Cost = x.Cost,
                Status = x.Status
            }).ToListAsync();

        return Ok(result);
    }
    
    [Authorize(Roles = "Employee")]
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<EquipmentResponse>>> GetMyEquipment()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        int userId = int.Parse(userIdClaim.Value);

        var result = await db.Equipment.Where(x => x.AssignedToUserId == userId && x.IsActive != false)
            .Select(x => new EquipmentShortResponse
            {
                Id = x.Id,
                Name = x.Name,
                Category = x.Category,
                Cost = x.Cost,
                Status = x.Status
            }).ToListAsync();

        return Ok(result);
    }
    
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentResponse>> GetById(int id)
    {
        var equipment = await db.Equipment.Include(x => x.Assignmenthistories)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive != false);

        if (equipment == null)
            return NotFound();

        var response = new EquipmentResponse
        {
            Id = equipment.Id,
            InventoryNumber = equipment.InventoryNumber,
            Name = equipment.Name,
            Description = equipment.Description,
            Category = equipment.Category,
            Status = equipment.Status,
            AssignedToUserId = equipment.AssignedToUserId,
            DateAssigned = equipment.DateAssigned,
            LastInventoryDate = equipment.LastInventoryDate,
            PurchaseDate = equipment.PurchaseDate,
            Cost = equipment.Cost,
            CreatedAt = equipment.CreatedAt,
            IsActive = equipment.IsActive,
            AssignmentHistory = new List<AssignmentHistoryResponse>()
        };

        response.AssignmentHistory = equipment.Assignmenthistories.Select(a => new AssignmentHistoryResponse
            {
                Id = a.Id,
                PreviousUserId = a.PreviousUserId,
                NewUserId = a.NewUserId,
                AssignedByAccountantId = a.AssignedByAccountantId,
                AssignmentDate = a.AssignmentDate,
                Action = a.Action,
                Reason = a.Reason
            }).ToList();

        return Ok(response);
    }

    [Authorize(Roles = "Accountant")]
    [HttpPost]
    public async Task<ActionResult<EquipmentResponse>> CreateEquipment([FromBody] EquipmentCreateRequest request)
    {
        if (await db.Equipment.AnyAsync(e => e.InventoryNumber == request.InventoryNumber))
            return Conflict("Оборудование с этим инвентарным номером уже существует..");

        var equipment = new Equipment
        {
            InventoryNumber = request.InventoryNumber,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Cost = request.Cost,
            PurchaseDate = request.PurchaseDate,
            LastInventoryDate = request.LastInventoryDate,
            Status = "Available",
            AssignedToUserId = null,
            DateAssigned = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Equipment.Add(equipment);
        await db.SaveChangesAsync();

        var response = new EquipmentResponse
        {
            Id = equipment.Id,
            InventoryNumber = equipment.InventoryNumber,
            Name = equipment.Name,
            Description = equipment.Description,
            Category = equipment.Category,
            Status = equipment.Status,
            AssignedToUserId = equipment.AssignedToUserId,
            DateAssigned = equipment.DateAssigned,
            LastInventoryDate = equipment.LastInventoryDate,
            PurchaseDate = equipment.PurchaseDate,
            Cost = equipment.Cost,
            CreatedAt = equipment.CreatedAt,
            IsActive = equipment.IsActive,
            AssignmentHistory = new List<AssignmentHistoryResponse>()
        };

        return CreatedAtAction(nameof(GetById), new { id = equipment.Id }, response);
    }

    [Authorize(Roles = "Accountant")]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEquipment(int id, [FromBody] EquipmentUpdateRequest request)
    {
        var equipment = await db.Equipment.FindAsync(id);
        if (equipment == null || equipment.IsActive == false) 
            return NotFound();

        equipment.Name = request.Name;
        equipment.Description = request.Description;
        equipment.Category = request.Category;
        equipment.Cost = request.Cost;
        equipment.PurchaseDate = request.PurchaseDate;
        equipment.LastInventoryDate = request.LastInventoryDate;

        await db.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Roles = "Accountant")]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] EquipmentStatusUpdateRequest request)
    {
        var allowedStatuses = new[] { "Available","Assigned","UnderRepair","Missing","Decommissioned" };
        if (!allowedStatuses.Contains(request.Status))
            return BadRequest("Неверный статус");

        var equipment = await db.Equipment.FindAsync(id);
        if (equipment == null || equipment.IsActive == false) 
            return NotFound();

        if(equipment.AssignedToUserId == null && request.Status == "Assigned")
            return BadRequest("К оборудованию ни кто не назначен");
        
        equipment.Status = request.Status;
        await db.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Roles = "Accountant")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEquipment(int id)
    {
        var equipment = await db.Equipment.FindAsync(id);
        if (equipment == null || equipment.IsActive == false) 
            return NotFound();

        equipment.IsActive = false;
        await db.SaveChangesAsync();
        return Ok();
    }
}