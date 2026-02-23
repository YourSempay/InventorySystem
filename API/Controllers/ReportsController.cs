using API.DB;
using API.Models.DTO.Reports;
using API.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace API.Controllers;

[ApiController]
[EnableRateLimiting("fixed")]
[Route("api/reports")]
[Authorize(Roles = "Accountant")]
public class ReportsController : Controller
{
    private readonly _1135InventorySystemContext db;
    private readonly ISystemSettingsService settings;

    public ReportsController(_1135InventorySystemContext db, ISystemSettingsService settings)
    {
        this.db = db;
        this.settings = settings;
    }

    [HttpGet("inventory-summary")]
    public async Task<ActionResult<List<InventorySummaryResponse>>> GetInventorySummary([FromQuery] int? employeeId,
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = db.Inventoryrecords.AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(x => x.EmployeeId == employeeId.Value);

        if (startDate.HasValue)
            query = query.Where(x => x.InventoryDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(x => x.InventoryDate <= endDate.Value);

        var result = await query.GroupBy(x => x.EmployeeId)
            .Select(g => new InventorySummaryResponse
            {
                EmployeeId = g.Key,
                TotalRecords = g.Count(),
                MissingCount = g.Count(x => x.IsPresent == false),
                LastInventory = g.Max(x => x.InventoryDate)
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("missing-equipment")]
    public async Task<ActionResult<List<MissingEquipmentResponse>>> GetMissingEquipment([FromQuery] bool? includeResolved = false)
    {
        bool includeMissingFromSettings = await settings.GetSettingValueAsBoolAsync("IncludeMissingEquipment");
    
        bool includeResolvedFinal = includeResolved ?? includeMissingFromSettings;
        
        List<Inventoryrecord> records;

        if (!includeResolvedFinal)
        {
            records = await db.Inventoryrecords.Where(r => r.InventoryDate == db.Inventoryrecords
                .Where(x => x.EquipmentId == r.EquipmentId).Max(x => x.InventoryDate)).Where(r => !r.IsPresent).ToListAsync();
        }
        else
        {
            records = await db.Inventoryrecords.Where(r => !r.IsPresent).ToListAsync();
        }

        var result = records.Select(x => new MissingEquipmentResponse
        {
            EquipmentId = x.EquipmentId,
            EmployeeId = x.EmployeeId,
            InventoryDate = x.InventoryDate,
            Location = x.Location,
            Comments = x.Comments
        });

        return Ok(result);
    }

    [HttpGet("equipment-status")]
    public async Task<ActionResult<List<EquipmentStatusResponse>>> GetEquipmentStatusDistribution()
    {
        var result = await db.Equipment.Where(x => x.IsActive != false).GroupBy(x => x.Status)
            .Select(x => new EquipmentStatusResponse
            {
                Status = x.Key,
                Count = x.Count()
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("assignment-history")]
    public async Task<ActionResult<List<AssignmentHistoryReportResponse>>> GetAssignmentHistory(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = db.Assignmenthistories.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(x => x.AssignmentDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(x => x.AssignmentDate <= endDate.Value);

        var history = await query.Include(x => x.Equipment)
            .Include(x => x.NewUser).Include(x => x.AssignedByAccountant)
            .Select(x => new AssignmentHistoryReportResponse
            {
                Id = x.Id,
                EquipmentName = x.Equipment.Name,
                Action = x.Action,
                PreviousUserId = x.PreviousUserId,
                NewUser = x.NewUser.FullName,
                Accountant = x.AssignedByAccountant.FullName,
                AssignmentDate = x.AssignmentDate,
                Reason = x.Reason
            }).OrderByDescending(x => x.AssignmentDate).ToListAsync();

        return Ok(history);
    }

    [HttpPost("export")]
    public async Task<ActionResult> ExportReport([FromBody] ExportReportRequest request)
    {
        var reportData = await GetInventorySummaryAsync(request.EmployeeId, request.StartDate, request.EndDate);
        
        var defaultFormat = await settings.GetSettingValueAsync("DefaultReportFormat");
        if (string.IsNullOrEmpty(request.Type))
            request.Type = defaultFormat;
        
        if (request.Type == "PDF")
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, request, reportData));
                    page.Content().Element(c => ComposeContent(c, reportData));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Страница ");
                        text.CurrentPageNumber();
                        text.Span(" из ");
                        text.TotalPages();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;
            var fileName = $"InventoryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }
        else if (request.Type == "Excel")
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Инвентаризация");

            ws.Cell(1, 1).Value = "ОТЧЁТ ПО ИНВЕНТАРИЗАЦИИ ОБОРУДОВАНИЯ";
            var range = ws.Range(1, 1, 1, 7);
            range.Merge();
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 12;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Fill.BackgroundColor = XLColor.FromArgb(42, 113, 192);

            ws.Cell(2, 1).Value = $"Период: {request.StartDate:dd.MM.yyyy} - {request.EndDate:dd.MM.yyyy}";
            if (request.EmployeeId.HasValue)
                ws.Cell(3, 1).Value = $"Сотрудник: {reportData.EmployeeName}";

            var headers = new[]
            {
                "Инв. номер", "Наименование", "Категория", "Состояние", "Присутствует", "Дата инвентаризации",
                "Местоположение"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(5, i + 1).Value = headers[i];
                ws.Cell(5, i + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            int row = 6;
            foreach (var item in reportData.Details)
            {
                ws.Cell(row, 1).Value = item.InventoryNumber;
                ws.Cell(row, 2).Value = item.EquipmentName;
                ws.Cell(row, 3).Value = item.Category;
                ws.Cell(row, 4).Value = item.EquipmentCondition;

                var presentCell = ws.Cell(row, 5);
                presentCell.Value = item.IsPresent ? "Да" : "НЕТ";
                presentCell.Style.Font.SetBold();
                presentCell.Style.Font.FontColor = item.IsPresent ? XLColor.Green : XLColor.Red;

                ws.Cell(row, 6).Value = item.InventoryDate;
                ws.Cell(row, 6).Style.DateFormat.Format = "dd.MM.yyyy";
                ws.Cell(row, 7).Value = item.Location;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            var fileName = $"InventoryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        else
        {
            return BadRequest("Поддерживаемые типы: PDF, Excel");
        }
    }


    private void ComposeHeader(IContainer container, ExportReportRequest request, InventoryReportResponse reportData)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("СИСТЕМА ИНВЕНТАРИЗАЦИОННОГО УЧЁТА").FontSize(16).Bold();
                col.Item().Text($"Период: {request.StartDate:dd.MM.yyyy} - {request.EndDate:dd.MM.yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);
                if (request.EmployeeId.HasValue)
                    col.Item().Text($"Сотрудник: {reportData.EmployeeName}").FontSize(10);
            });
        });
    }

    private void ComposeContent(IContainer container, InventoryReportResponse reportData)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Item().Component(new SummaryBox(reportData.TotalEquipment, "Всего историй оборудования"));
            col.Item().Component(new SummaryBox(reportData.InventoriedCount, "Присутсвует"));
            col.Item().Component(new SummaryBox(reportData.MissingCount, "Отсутствует", true));

            col.Item().PaddingTop(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(100);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Инв. номер").Bold();
                    header.Cell().Element(CellStyle).Text("Наименование").Bold();
                    header.Cell().Element(CellStyle).Text("Состояние").Bold();
                    header.Cell().Element(CellStyle).Text("Присутствует").Bold();
                    header.Cell().Element(CellStyle).Text("Дата").Bold();
                });

                foreach (var item in reportData.Details)
                {
                    table.Cell().Element(CellStyle).Text(item.InventoryNumber);
                    table.Cell().Element(CellStyle).Text(item.EquipmentName);
                    table.Cell().Element(CellStyle).Text(item.EquipmentCondition).FontColor(item.EquipmentCondition == "Unusable" ? Colors.Red.Medium : Colors.Black);
                    table.Cell().Element(CellStyle).Text(item.IsPresent ? "Да" : "Нет").FontColor(item.IsPresent ? Colors.Green.Medium : Colors.Red.Medium);
                    table.Cell().Element(CellStyle).Text(item.InventoryDate.ToString("dd.MM.yyyy"));
                }
            });

            col.Item().PaddingTop(20).AlignRight().Text($"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm}")
                .FontSize(9).FontColor(Colors.Grey.Medium);
        });
    }

    private static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter();

    internal class SummaryBox : IComponent
    {
        private readonly int _value;
        private readonly string _label;
        private readonly bool _isCritical;

        public SummaryBox(int value, string label, bool isCritical = false)
        {
            _value = value;
            _label = label;
            _isCritical = isCritical;
        }

        public void Compose(IContainer container)
        {
            container.Border(1).BorderColor(_isCritical ? Colors.Red.Medium : Colors.Blue.Medium).Background(_isCritical ? Colors.Red.Lighten4 : Colors.Blue.Lighten4)
                .Padding(8).Width(150).Column(col =>
                {
                    col.Item().Text(_value.ToString()).FontSize(24).Bold()
                        .FontColor(_isCritical ? Colors.Red.Medium : Colors.Blue.Medium);
                    col.Item().Text(_label).FontSize(10).FontColor(Colors.Black);
                });
        }
    }

    private async Task<InventoryReportResponse> GetInventorySummaryAsync(int? employeeId, DateTime startDate,
        DateTime endDate)
    {
        var query = db.Inventoryrecords.Include(x => x.Equipment).Include(x => x.Employee)
            .Where(x => x.InventoryDate >= startDate && x.InventoryDate <= endDate);

        if (employeeId.HasValue)
            query = query.Where(x => x.EmployeeId == employeeId.Value);

        var details = await query.Select(x => new InventoryReportDetail
        {
            InventoryNumber = x.Equipment.InventoryNumber,
            EquipmentName = x.Equipment.Name,
            Category = x.Equipment.Category,
            EquipmentCondition = x.EquipmentCondition,
            IsPresent = x.IsPresent,
            InventoryDate = x.InventoryDate,
            Location = x.Location
        }).ToListAsync();

        var employeeName = employeeId.HasValue
            ? (await db.Users.Where(u => u.Id == employeeId.Value).Select(u => u.FullName).FirstOrDefaultAsync())
            : "Все";

        return new InventoryReportResponse
        {
            EmployeeName = employeeName,
            TotalEquipment = details.Count,
            InventoriedCount = details.Count(d => d.IsPresent),
            MissingCount = details.Count(d => !d.IsPresent),
            Details = details
        };
    }
}