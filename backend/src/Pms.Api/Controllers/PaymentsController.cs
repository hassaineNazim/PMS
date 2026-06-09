using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Billing;
using Pms.Application.Features.Payments;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PaymentsController(IPaymentService payments, IFolioService folio) : ControllerBase
{
    [HttpGet("reservations/{reservationId:guid}/folio")]
    public async Task<ActionResult<FolioDto>> Folio(Guid reservationId, CancellationToken ct)
        => Ok(await folio.GetAsync(reservationId, ct));

    [HttpPost("payments")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public async Task<ActionResult<PaymentResultDto>> Record(RecordPaymentRequest request, CancellationToken ct)
        => Ok(await payments.RecordAsync(request, ct));

    [HttpDelete("payments/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await payments.DeleteAsync(id, ct);
        return NoContent();
    }
}
