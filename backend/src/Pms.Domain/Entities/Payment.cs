using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>
/// A payment movement against a reservation/folio. A reservation can have several
/// payments (deposit at booking + balance at departure). Distinct from an Invoice
/// (which states what is owed); payments state what was actually received.
/// </summary>
public class Payment : TenantEntity
{
    public Guid ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public Guid? GuestId { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentType Type { get; set; } = PaymentType.Balance;

    /// <summary>Droit de timbre charged on this payment (cash only).</summary>
    public decimal StampDuty { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid? CashSessionId { get; set; }
    public Guid? UserId { get; set; }

    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Signed effect on the cash drawer / amount received (+ in, − refund).</summary>
    public decimal SignedAmount => Type == PaymentType.Refund ? -Amount : Amount;
}
