using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Settings;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin,Manager")]
public class SettingsController(ISettingsService settings) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TenantSettingsDto>> Get(CancellationToken ct)
        => Ok(await settings.GetAsync(ct));

    [HttpPut]
    public async Task<ActionResult<TenantSettingsDto>> Update(TenantSettingsDto dto, CancellationToken ct)
        => Ok(await settings.UpdateAsync(dto, ct));
}
