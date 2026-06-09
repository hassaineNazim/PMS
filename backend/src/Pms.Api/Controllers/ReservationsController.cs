using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Reservations;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController(IReservationService reservations) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReservationDto>>> GetAll(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await reservations.GetAllAsync(from, to, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> Get(Guid id, CancellationToken ct)
        => Ok(await reservations.GetByIdAsync(id, ct));

    [HttpPost("availability")]
    public async Task<ActionResult<IReadOnlyList<AvailableRoomDto>>> Availability(
        AvailabilityRequest request, CancellationToken ct)
        => Ok(await reservations.GetAvailableRoomsAsync(request, ct));

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationRequest request, CancellationToken ct)
    {
        var res = await reservations.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = res.Id }, res);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> Update(Guid id, UpdateReservationRequest request, CancellationToken ct)
        => Ok(await reservations.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(Guid id, CancellationToken ct)
        => Ok(await reservations.CancelAsync(id, ct));
}
