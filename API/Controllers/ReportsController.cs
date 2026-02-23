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
    public async Task<ActionResult<List<MissingEquipmentResponse>>> GetMissingEquipment(
        [FromQuery] bool? includeResolved = false)
    {
        bool includeMissingFromSettings = await settings.GetSettingValueAsBoolAsync("IncludeMissingEquipment");

        bool includeResolvedFinal = includeResolved ?? includeMissingFromSettings;

        List<Inventoryrecord> records;

        if (!includeResolvedFinal)
        {
            records = await db.Inventoryrecords.Where(r => r.InventoryDate == db.Inventoryrecords
                    .Where(x => x.EquipmentId == r.EquipmentId).Max(x => x.InventoryDate)).Where(r => !r.IsPresent)
                .ToListAsync();
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
            return GeneratePdf(reportData, request);

        if (request.Type == "Excel")
            return GenerateExcel(reportData, request);

        return BadRequest("Поддерживаемые типы: PDF, Excel");
    }

    private ActionResult GeneratePdf(InventoryReportResponse data, ExportReportRequest request)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => PdfHeader(c, request, data));
                page.Content().Element(c => PdfContent(c, data));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Страница ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);

        return File(stream.ToArray(), "application/pdf", $"InventoryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    private void PdfHeader(IContainer container, ExportReportRequest request, InventoryReportResponse data)
    {
        container.Column(col =>
        {
            col.Item().Text("ОТЧЁТ ПО ИНВЕНТАРИЗАЦИИ").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().Text($"Период: {request.StartDate:dd.MM.yyyy} - {request.EndDate:dd.MM.yyyy}");
            col.Item().Text($"Сотрудник: {data.EmployeeName}");
        });
    }

    private void PdfContent(IContainer container, InventoryReportResponse data)
    {
        container.Background(Colors.Grey.Darken4).Padding(20).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(150).Background(Colors.Purple.Medium).Padding(10).Column(c =>
                {
                    c.Item().Text("ИНВЕНТАРИЗАЦИЯ").FontSize(18).Bold().FontColor(Colors.White);
                    c.Item().Text(data.EmployeeName).FontSize(10).FontColor(Colors.White);
                });

                row.RelativeItem().PaddingLeft(10).Column(c =>
                {
                    c.Item().Row(summaryRow =>
                    {
                        summaryRow.RelativeItem()
                            .Component(new NeonSummaryCard(data.TotalEquipment, "Всего", Colors.Cyan.Medium));
                        summaryRow.RelativeItem()
                            .Component(new NeonSummaryCard(data.InventoriedCount, "В наличии", Colors.Lime.Medium));
                        summaryRow.RelativeItem()
                            .Component(new NeonSummaryCard(data.MissingCount, "Отсутствует", Colors.Red.Medium));
                    });

                    c.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            Color[] colors =
                            {
                                Colors.Purple.Medium, Colors.Cyan.Medium, Colors.Lime.Medium, Colors.Orange.Medium,
                                Colors.Pink.Medium
                            };
                            string[] titles = { "Инв. номер", "Наименование", "Состояние", "Наличие", "Дата" };
                            for (int i = 0; i < titles.Length; i++)
                            {
                                header.Cell().Background(colors[i]).Padding(5).Text(titles[i]).FontColor(Colors.White)
                                    .Bold();
                            }
                        });

                        int index = 0;
                        foreach (var item in data.Details)
                        {
                            var bg = index % 2 == 0 ? Colors.Grey.Darken3 : Colors.Grey.Darken2;

                            table.Cell().Element(c => c.Background(bg).Padding(5)).Text(item.InventoryNumber);
                            table.Cell().Element(c => c.Background(bg).Padding(5)).Text(item.EquipmentName);
                            table.Cell().Element(c => c.Background(bg).Padding(5)).Text(item.EquipmentCondition);
                            table.Cell().Element(c => c.Background(bg).Padding(5))
                                .Text(item.IsPresent ? "Да" : "Нет")
                                .FontColor(item.IsPresent ? Colors.Lime.Medium : Colors.Red.Medium);
                            table.Cell().Element(c => c.Background(bg).Padding(5))
                                .Text(item.InventoryDate.ToString("dd.MM.yyyy"));

                            index++;
                        }
                    });

                    c.Item().AlignRight().Text($"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Lighten2);
                });
            });
        });
    }

    internal class NeonSummaryCard : IComponent
    {
        private readonly int value;
        private readonly string label;
        private readonly Color color;

        public NeonSummaryCard(int value, string label, Color color)
        {
            this.value = value;
            this.label = label;
            this.color = color;
        }

        public void Compose(IContainer container)
        {
            container.Border(1)
                .BorderColor(color)
                .Background(Colors.Black)
                .Padding(10)
                .Column(col =>
                {
                    col.Item().Text(value.ToString()).FontSize(24).Bold().FontColor(color);
                    col.Item().Text(label).FontSize(12).FontColor(Colors.White);
                });
        }
    }

    private static IContainer HeaderStyle(IContainer container) =>
        container.Background(Colors.Blue.Medium).Padding(5).AlignCenter()
            .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

    private static IContainer CellStyle(IContainer container, string bgColor) =>
        container.Background(bgColor).Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).AlignCenter();

    internal class SummaryCard : IComponent
    {
        private readonly int value;
        private readonly string label;
        private readonly bool critical;

        public SummaryCard(int value, string label, bool critical = false)
        {
            this.value = value;
            this.label = label;
            this.critical = critical;
        }

        public void Compose(IContainer container)
        {
            container.Border(1)
                .BorderColor(critical ? Colors.Red.Medium : Colors.Blue.Medium)
                .Background(critical ? Colors.Red.Lighten4 : Colors.Blue.Lighten4)
                .Padding(10)
                .Column(col =>
                {
                    col.Item().Text(value.ToString()).FontSize(22).Bold()
                        .FontColor(critical ? Colors.Red.Medium : Colors.Blue.Medium);
                    col.Item().Text(label);
                });
        }
    }


    private ActionResult GenerateExcel(InventoryReportResponse data, ExportReportRequest request)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Отчёт");

        ws.Cell("A1").Value = "ОТЧЁТ ПО ИНВЕНТАРИЗАЦИИ";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;

        ws.Cell("E1").Value = "Период:";
        ws.Cell("F1").Value = $"{request.StartDate:dd.MM.yyyy} - {request.EndDate:dd.MM.yyyy}";
        ws.Cell("E2").Value = "Сотрудник:";
        ws.Cell("F2").Value = data.EmployeeName;

        string[] headers = { "Инв. номер", "Наименование", "Категория", "Состояние", "Наличие", "Дата", "Локация" };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(30, 58, 138);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 5;
        bool alternate = false;

        foreach (var item in data.Details)
        {
            if (alternate) ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(243, 244, 246);

            ws.Cell(row, 1).Value = item.InventoryNumber;
            ws.Cell(row, 2).Value = item.EquipmentName;
            ws.Cell(row, 3).Value = item.Category;
            ws.Cell(row, 4).Value = item.EquipmentCondition;

            var presence = ws.Cell(row, 5);
            presence.Value = item.IsPresent ? "Да" : "Нет";
            presence.Style.Font.Bold = true;
            presence.Style.Font.FontColor = item.IsPresent ? XLColor.DarkGreen : XLColor.DarkRed;

            ws.Cell(row, 6).Value = item.InventoryDate;
            ws.Cell(row, 6).Style.DateFormat.Format = "dd.MM.yyyy";

            ws.Cell(row, 7).Value = item.Location;

            alternate = !alternate;
            row++;
        }

        ws.Range(4, 1, row - 1, headers.Length).SetAutoFilter();
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"InventoryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
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