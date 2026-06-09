using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Reports;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(IReportService reports) : ControllerBase
{
    [HttpGet("main-courante")]
    public async Task<ActionResult<IReadOnlyList<MainCouranteEntryDto>>> MainCourante([FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(await reports.GetMainCouranteAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), ct));

    [HttpGet("police-form/{reservationId:guid}")]
    public async Task<IActionResult> PoliceForm(Guid reservationId, CancellationToken ct)
    {
        var pdf = await reports.GeneratePoliceFormAsync(reservationId, ct);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    [HttpGet("reservations.csv")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ReservationsCsv([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => File(await reports.ExportReservationsCsvAsync(from, to, ct), "text/csv", "reservations.csv");

    [HttpGet("revenue.csv")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RevenueCsv(CancellationToken ct)
        => File(await reports.ExportRevenueCsvAsync(ct), "text/csv", "revenue.csv");
}
