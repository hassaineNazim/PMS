using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.CheckIn;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class CheckInController(ICheckInService checkIn) : ControllerBase
{
    [HttpPost("checkin/{reservationId:guid}")]
    public async Task<ActionResult<CheckInResult>> CheckIn(
        Guid reservationId, CheckInRequest request, CancellationToken ct)
        => Ok(await checkIn.CheckInAsync(reservationId, request, ct));

    [HttpPost("checkout/{reservationId:guid}")]
    public async Task<ActionResult<CheckOutResult>> CheckOut(Guid reservationId, CancellationToken ct)
        => Ok(await checkIn.CheckOutAsync(reservationId, ct));
}
