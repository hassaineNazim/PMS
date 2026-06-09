using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.CashRegister;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/cash")]
[Authorize(Roles = "Admin,Manager,Receptionist")]
public class CashRegisterController(ICashRegisterService cash) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<CashSessionDto?>> Current(CancellationToken ct)
        => Ok(await cash.GetCurrentAsync(ct));

    [HttpPost("open")]
    public async Task<ActionResult<CashSessionDto>> Open(OpenCashSessionRequest request, CancellationToken ct)
        => Ok(await cash.OpenAsync(request, ct));

    [HttpPost("close")]
    public async Task<ActionResult<CashSessionDto>> Close(CloseCashSessionRequest request, CancellationToken ct)
        => Ok(await cash.CloseAsync(request, ct));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<CashSessionDto>>> History(CancellationToken ct)
        => Ok(await cash.GetHistoryAsync(ct));
}
