using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Application.Features.Charges;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Billing;

public record PaymentLineDto(Guid Id, decimal Amount, PaymentMethod Method, PaymentType Type,
    decimal StampDuty, string? Reference, DateTimeOffset PaidAt);

public record FolioDto(
    Guid ReservationId,
    string GuestName,
    string RoomNumber,
    MealPlan MealPlan,
    decimal RoomSubtotal,
    decimal MealPlanSubtotal,
    decimal ExtrasSubtotal,
    decimal Subtotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal StampDuty,
    decimal Total,
    decimal AmountPaid,
    decimal BalanceDue,
    string Currency,
    IReadOnlyList<ChargeDto> Charges,
    IReadOnlyList<PaymentLineDto> Payments);

public interface IFolioService
{
    Task<FolioDto> GetAsync(Guid reservationId, CancellationToken ct = default);

    /// <summary>Recomputes and persists the invoice attached to a reservation (extras, stamp, paid).</summary>
    Task RefreshInvoiceAsync(Guid reservationId, CancellationToken ct = default);
}

public class FolioService(IApplicationDbContext db, ICurrentTenant tenant) : IFolioService
{
    public async Task<FolioDto> GetAsync(Guid reservationId, CancellationToken ct = default)
    {
        var res = await db.Reservations.Include(r => r.Guest).Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new NotFoundException(nameof(Reservation), reservationId);

        var t = await db.Tenants.IgnoreQueryFilters().FirstAsync(x => x.Id == tenant.TenantId, ct);
        var charges = await db.Charges.Where(c => c.ReservationId == reservationId).OrderBy(c => c.PostedAt).ToListAsync(ct);
        var payments = await db.Payments.Where(p => p.ReservationId == reservationId).OrderBy(p => p.PaidAt).ToListAsync(ct);

        var f = Compute(res, t, charges, payments);

        return new FolioDto(
            res.Id, res.Guest?.FullName ?? "", res.Room?.Number ?? "", res.MealPlan,
            f.Room, f.Meal, f.Extras, f.Subtotal, t.DefaultTaxRate, f.Tax, f.Stamp, f.Total, f.Paid, f.Balance,
            t.Currency,
            charges.Select(c => new ChargeDto(c.Id, c.ReservationId, c.Category, c.Label, c.Quantity, c.UnitPrice, c.Total, c.PostedAt)).ToList(),
            payments.Select(p => new PaymentLineDto(p.Id, p.Amount, p.Method, p.Type, p.StampDuty, p.Reference, p.PaidAt)).ToList());
    }

    public async Task RefreshInvoiceAsync(Guid reservationId, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.ReservationId == reservationId, ct);
        if (invoice is null) return;

        var charges = await db.Charges.Where(c => c.ReservationId == reservationId).ToListAsync(ct);
        var payments = await db.Payments.Where(p => p.ReservationId == reservationId).ToListAsync(ct);

        invoice.ExtrasSubtotal = decimal.Round(charges.Sum(c => c.Total), 2);
        invoice.StampDuty = decimal.Round(payments.Sum(p => p.StampDuty), 2);
        invoice.AmountPaid = decimal.Round(payments.Sum(p => p.SignedAmount), 2);
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        invoice.Recalculate();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Pure folio math shared by the read model and the invoice refresh.</summary>
    public static (decimal Room, decimal Meal, decimal Extras, decimal Subtotal, decimal Tax, decimal Stamp, decimal Total, decimal Paid, decimal Balance)
        Compute(Reservation res, Tenant t, List<Charge> charges, List<Payment> payments)
    {
        var room = res.TotalAmount;
        var meal = res.MealPlanTotal;
        var extras = decimal.Round(charges.Sum(c => c.Total), 2);
        var subtotal = decimal.Round(room + meal + extras, 2);
        var tax = decimal.Round(subtotal * t.DefaultTaxRate / 100m, 2);
        var stamp = decimal.Round(payments.Sum(p => p.StampDuty), 2);
        var total = decimal.Round(subtotal + tax + stamp, 2);
        var paid = decimal.Round(payments.Sum(p => p.SignedAmount), 2);
        var balance = decimal.Round(total - paid, 2);
        return (room, meal, extras, subtotal, tax, stamp, total, paid, balance);
    }
}
