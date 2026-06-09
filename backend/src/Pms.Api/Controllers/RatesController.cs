using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Rates;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/rates")]
[Authorize(Roles = "Admin,Manager")]
public class RatesController(IRateService rates) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RatePeriodDto>>> GetAll(CancellationToken ct)
        => Ok(await rates.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<RatePeriodDto>> Create(SaveRatePeriodRequest request, CancellationToken ct)
        => Ok(await rates.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RatePeriodDto>> Update(Guid id, SaveRatePeriodRequest request, CancellationToken ct)
        => Ok(await rates.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await rates.DeleteAsync(id, ct);
        return NoContent();
    }
}
