using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Rooms;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController(IRoomService rooms) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(CancellationToken ct)
        => Ok(await rooms.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomDto>> Get(Guid id, CancellationToken ct)
        => Ok(await rooms.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomRequest request, CancellationToken ct)
    {
        var room = await rooms.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = room.Id }, room);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Receptionist,Housekeeping")]
    public async Task<ActionResult<RoomDto>> Update(Guid id, UpdateRoomRequest request, CancellationToken ct)
        => Ok(await rooms.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await rooms.DeleteAsync(id, ct);
        return NoContent();
    }
}
