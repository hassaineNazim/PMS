using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Staff;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class StaffController(IStaffService staff) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffDto>>> GetAll(CancellationToken ct)
        => Ok(await staff.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StaffDto>> Get(Guid id, CancellationToken ct)
        => Ok(await staff.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<StaffDto>> Create(CreateStaffRequest request, CancellationToken ct)
    {
        var created = await staff.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StaffDto>> Update(Guid id, UpdateStaffRequest request, CancellationToken ct)
        => Ok(await staff.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await staff.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<IReadOnlyList<ScheduleDto>>> GetSchedules(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await staff.GetSchedulesAsync(from, to, ct));

    [HttpPost("schedules")]
    public async Task<ActionResult<ScheduleDto>> CreateSchedule(CreateScheduleRequest request, CancellationToken ct)
        => Ok(await staff.CreateScheduleAsync(request, ct));

    [HttpDelete("schedules/{id:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid id, CancellationToken ct)
    {
        await staff.DeleteScheduleAsync(id, ct);
        return NoContent();
    }
}
