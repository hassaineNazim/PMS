using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Common;
using Pms.Application.Features.Guests;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GuestsController(IGuestService guests) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<GuestDto>>> Search(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await guests.SearchAsync(search, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GuestDto>> Get(Guid id, CancellationToken ct)
        => Ok(await guests.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<GuestDto>> Create(CreateGuestRequest request, CancellationToken ct)
    {
        var guest = await guests.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = guest.Id }, guest);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GuestDto>> Update(Guid id, UpdateGuestRequest request, CancellationToken ct)
        => Ok(await guests.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await guests.DeleteAsync(id, ct);
        return NoContent();
    }
}
