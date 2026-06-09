using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Charges;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/charges")]
[Authorize]
public class ChargesController(IChargeService charges) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChargeDto>>> ForReservation([FromQuery] Guid reservationId, CancellationToken ct)
        => Ok(await charges.GetForReservationAsync(reservationId, ct));

    [HttpPost]
    public async Task<ActionResult<ChargeDto>> Create(CreateChargeRequest request, CancellationToken ct)
        => Ok(await charges.CreateAsync(request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await charges.DeleteAsync(id, ct);
        return NoContent();
    }
}
