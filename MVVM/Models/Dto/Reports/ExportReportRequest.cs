using System;

namespace MVVM.Models.Dto.Reports;

public class ExportReportRequest
{
    public string Type { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}