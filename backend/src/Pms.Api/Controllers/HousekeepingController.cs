using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Housekeeping;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/housekeeping")]
[Authorize(Roles = "Admin,Manager,Housekeeping")]
public class HousekeepingController(IHousekeepingService housekeeping) : ControllerBase
{
    [HttpGet("board")]
    public async Task<ActionResult<IReadOnlyList<HousekeepingRoomDto>>> Board(CancellationToken ct)
        => Ok(await housekeeping.GetBoardAsync(ct));

    [HttpPut("{roomId:guid}/assign")]
    public async Task<ActionResult<HousekeepingRoomDto>> Assign(Guid roomId, AssignRequest request, CancellationToken ct)
        => Ok(await housekeeping.AssignAsync(roomId, request, ct));

    [HttpPut("{roomId:guid}/status")]
    public async Task<ActionResult<HousekeepingRoomDto>> SetStatus(Guid roomId, SetHousekeepingStatusRequest request, CancellationToken ct)
        => Ok(await housekeeping.SetStatusAsync(roomId, request, ct));
}
