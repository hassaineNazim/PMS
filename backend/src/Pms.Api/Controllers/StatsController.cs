using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Stats;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController(IStatsService stats) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> Dashboard(CancellationToken ct)
        => Ok(await stats.GetDashboardAsync(ct));
}
