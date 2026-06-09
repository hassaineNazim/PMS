using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Application.Features.Billing;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Payments;

public record RecordPaymentRequest(Guid ReservationId, decimal Amount, PaymentMethod Method,
    PaymentType Type, string? Reference, string? Notes);

public record PaymentResultDto(Guid Id, decimal Amount, PaymentMethod Method, PaymentType Type,
    decimal StampDuty, decimal NewBalanceDue);

public class RecordPaymentValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public interface IPaymentService
{
    Task<PaymentResultDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class PaymentService(
    IApplicationDbContext db,
    ICurrentTenant tenant,
    ICurrentUser user,
    IFolioService folio) : IPaymentService
{
    public async Task<PaymentResultDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException(nameof(Reservation), request.ReservationId);

        var t = await db.Tenants.IgnoreQueryFilters().FirstAsync(x => x.Id == tenant.TenantId, ct);

        // Droit de timbre applies to cash receipts only (not refunds).
        decimal stamp = (request.Method == PaymentMethod.Cash && request.Type != PaymentType.Refund)
            ? t.ComputeFiscalStamp(request.Amount)
            : 0m;

        // Attach cash movements to the user's currently open cash session, if any.
        Guid? sessionId = null;
        if (request.Method == PaymentMethod.Cash)
        {
            var session = await db.CashSessions.FirstOrDefaultAsync(
                s => s.Status == CashSessionStatus.Open && s.UserId == user.UserId, ct);
            sessionId = session?.Id;
        }

        var payment = new Payment
        {
            TenantId = tenant.TenantId,
            ReservationId = reservation.Id,
            GuestId = reservation.GuestId,
            InvoiceId = await db.Invoices.Where(i => i.ReservationId == reservation.Id)
                .Select(i => (Guid?)i.Id).FirstOrDefaultAsync(ct),
            Amount = request.Amount,
            Method = request.Method,
            Type = request.Type,
            StampDuty = stamp,
            Reference = request.Reference,
            Notes = request.Notes,
            CashSessionId = sessionId,
            UserId = user.UserId
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        await folio.RefreshInvoiceAsync(reservation.Id, ct);
        var f = await folio.GetAsync(reservation.Id, ct);

        return new PaymentResultDto(payment.Id, payment.Amount, payment.Method, payment.Type, stamp, f.BalanceDue);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Payment), id);
        var reservationId = payment.ReservationId;
        db.Payments.Remove(payment);
        await db.SaveChangesAsync(ct);
        await folio.RefreshInvoiceAsync(reservationId, ct);
    }
}
