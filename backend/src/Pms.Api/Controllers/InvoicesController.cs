using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Features.Invoices;
using Pms.Domain.Enums;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController(IInvoiceService invoices) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> GetAll(CancellationToken ct)
        => Ok(await invoices.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> Get(Guid id, CancellationToken ct)
        => Ok(await invoices.GetByIdAsync(id, ct));

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public async Task<ActionResult<InvoiceDto>> SetStatus(Guid id, [FromBody] InvoiceStatus status, CancellationToken ct)
        => Ok(await invoices.SetStatusAsync(id, status, ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var pdf = await invoices.GeneratePdfAsync(id, ct);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }
}
