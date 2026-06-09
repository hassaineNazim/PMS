using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Application.Integrations;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Invoices;

public class InvoiceService(
    IApplicationDbContext db,
    ICurrentTenant currentTenant,
    IInvoiceDocumentGenerator pdfGenerator) : IInvoiceService
{
    public async Task<IReadOnlyList<InvoiceDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Invoices
            .Include(i => i.Guest).Include(i => i.Room)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => Map(i)).ToListAsync(ct);

    public async Task<InvoiceDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await LoadAsync(id, ct);
        return Map(invoice);
    }

    public async Task<InvoiceDto> SetStatusAsync(Guid id, InvoiceStatus status, CancellationToken ct = default)
    {
        var invoice = await LoadAsync(id, ct);
        invoice.Status = status;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(invoice);
    }

    public async Task<InvoicePdf> GeneratePdfAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await LoadAsync(id, ct);
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == currentTenant.TenantId, ct)
            ?? throw new NotFoundException(nameof(Tenant), currentTenant.TenantId);

        var charges = await db.Charges.Where(c => c.ReservationId == invoice.ReservationId)
            .OrderBy(c => c.PostedAt).ToListAsync(ct);

        var bytes = pdfGenerator.Generate(invoice, tenant, invoice.Guest!, invoice.Room!, charges);
        return new InvoicePdf($"{invoice.Number}.pdf", bytes);
    }

    private async Task<Invoice> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Invoices.Include(i => i.Guest).Include(i => i.Room)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
        ?? throw new NotFoundException(nameof(Invoice), id);

    private static InvoiceDto Map(Invoice i) => new(
        i.Id, i.Number, i.ReservationId, i.GuestId, i.Guest?.FullName ?? string.Empty,
        i.RoomId, i.Room?.Number ?? string.Empty, i.CheckIn, i.CheckOut, i.Nights,
        i.PricePerNight, i.RoomSubtotal, i.MealPlanSubtotal, i.ExtrasSubtotal, i.Subtotal,
        i.TaxRate, i.TaxAmount, i.StampDuty, i.Total, i.AmountPaid, i.BalanceDue,
        i.Currency, i.Status, i.CreatedAt);
}
