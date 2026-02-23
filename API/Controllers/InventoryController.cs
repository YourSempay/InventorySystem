using API.DB;
using API.Models.DTO.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using API.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[EnableRateLimiting("fixed")]
[Route("api/inventory")]
[Authorize]
public class InventoryController : Controller
{
    private readonly _1135InventorySystemContext db;
    private readonly string[] AllowedConditions = new[] { "New", "Good", "RequiresRepair", "Unusable" };
    private readonly ISystemSettingsService settings;

    public InventoryController(_1135InventorySystemContext db, ISystemSettingsService settings)
    {
        this.db = db;
        this.settings = settings;
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("my")]
    public async Task<ActionResult<List<InventoryRecordResponse>>> GetMyInventory()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var records = await db.Inventoryrecords.Where(x => x.EmployeeId == userId)
            .Select(x => new InventoryRecordResponse
            {
                Id = x.Id,
                EquipmentId = x.EquipmentId,
                EmployeeId = x.EmployeeId,
                EquipmentCondition = x.EquipmentCondition,
                IsPresent = x.IsPresent,
                Location = x.Location,
                Comments = x.Comments,
                PhotoPath = x.PhotoPath != null ? $"{Request.Scheme}://{Request.Host}/{x.PhotoPath}" : null,
                InventoryDate = x.InventoryDate
            }).ToListAsync();

        return Ok(records);
    }

    [Authorize(Roles = "Accountant")]
    [HttpGet("equipment/{id}")]
    public async Task<ActionResult<List<InventoryRecordResponse>>> GetEquipmentHistory(int id)
    {
        var records = await db.Inventoryrecords.Where(x => x.EquipmentId == id)
            .Select(x => new InventoryRecordResponse
            {
                Id = x.Id,
                EquipmentId = x.EquipmentId,
                EmployeeId = x.EmployeeId,
                EquipmentCondition = x.EquipmentCondition,
                IsPresent = x.IsPresent,
                Location = x.Location,
                Comments = x.Comments,
                PhotoPath = x.PhotoPath != null ? $"{Request.Scheme}://{Request.Host}/{x.PhotoPath}" : null,
                InventoryDate = x.InventoryDate
            }).ToListAsync();

        return Ok(records);
    }

    [Authorize(Roles = "Employee")]
    [HttpPost]
    public async Task<ActionResult> CreateInventory([FromForm] InventoryCreateRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var equipment = await db.Equipment.FindAsync(request.EquipmentId);
        if (equipment == null || equipment.AssignedToUserId != userId)
            return BadRequest("Нельзя создать запись: оборудование не назначено вам");


        int intervalHours = await settings.GetSettingValueAsIntAsync("InventoryCheckIntervalHours");
        var lastInterval= DateTime.UtcNow.AddHours(-intervalHours);
        
        bool exists = await db.Inventoryrecords.AnyAsync(x => x.EquipmentId == request.EquipmentId 
         && x.EmployeeId == userId && x.InventoryDate >= lastInterval );
        
        if (exists)
            return BadRequest($"Запись уже создана за последние {intervalHours} часа");

        if (!AllowedConditions.Contains(request.EquipmentCondition))
            return BadRequest("Неверное состояние оборудования. Допустимые: New, Good, RequiresRepair, Unusable");

        var record = new Inventoryrecord
        {
            EquipmentId = request.EquipmentId,
            EmployeeId = userId,
            EquipmentCondition = request.EquipmentCondition,
            IsPresent = request.IsPresent,
            Location = request.Location,
            Comments = request.Comments,
            InventoryDate = DateTime.UtcNow
        };
        
        if (request.PhotoPath != null)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.PhotoPath.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.PhotoPath.CopyToAsync(stream);
            }

            record.PhotoPath = Path.Combine($"uploads/{fileName}");
        }

        if (!record.IsPresent)
            equipment.Status = "Missing";
        else if (record.EquipmentCondition == "Unusable")
            equipment.Status = "UnderRepair";
        else
            equipment.Status = "Assigned";
        
        db.Inventoryrecords.Add(record);
        await db.SaveChangesAsync();

        return Ok();
    }

    [Authorize(Roles = "Accountant")]
    [HttpPut("{id}/correct")]
    public async Task<ActionResult> CorrectInventory(int id, [FromForm] InventoryRecordRequest request)
    {
        var record = await db.Inventoryrecords.FindAsync(id);
        if (record == null)
            return NotFound();

        if (!AllowedConditions.Contains(request.EquipmentCondition))
            return BadRequest("Неверное состояние оборудования. Допустимые: New, Good, RequiresRepair, Unusable");

        record.EquipmentCondition = request.EquipmentCondition;
        record.IsPresent = request.IsPresent;
        record.Location = request.Location;
        record.Comments = request.Comments;
        if (request.PhotoPath != null)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.PhotoPath.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.PhotoPath.CopyToAsync(stream);
            }

            record.PhotoPath = Path.Combine($"uploads/{fileName}");
        }
        await db.SaveChangesAsync();
        return Ok();
    }
}