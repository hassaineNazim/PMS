using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Invoice : TenantEntity
{
    /// <summary>Human-readable sequential number per tenant, e.g. "INV-2026-000042".</summary>
    public string Number { get; set; } = string.Empty;

    public Guid ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public Guid GuestId { get; set; }
    public Guest? Guest { get; set; }

    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights { get; set; }

    public decimal PricePerNight { get; set; }

    // Line groups (pre-tax)
    public decimal RoomSubtotal { get; set; }
    public decimal MealPlanSubtotal { get; set; }
    public decimal ExtrasSubtotal { get; set; }
    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>Droit de timbre (fiscal stamp) accumulated on cash payments.</summary>
    public decimal StampDuty { get; set; }

    public decimal Total { get; set; }

    // Settlement
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }

    public string Currency { get; set; } = "DZD";

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

    /// <summary>
    /// Recomputes the folio: subtotal = room + meal plan + extras, plus tax and
    /// the (cash) fiscal stamp, then the balance after payments.
    /// </summary>
    public void Recalculate()
    {
        // RoomSubtotal is set by the caller (it reflects seasonal rates, not just
        // Nights × base price). Fall back to the simple computation if unset.
        if (RoomSubtotal == 0) RoomSubtotal = decimal.Round(Nights * PricePerNight, 2);
        Subtotal = decimal.Round(RoomSubtotal + MealPlanSubtotal + ExtrasSubtotal, 2);
        TaxAmount = decimal.Round(Subtotal * TaxRate / 100m, 2);
        Total = decimal.Round(Subtotal + TaxAmount + StampDuty, 2);
        BalanceDue = decimal.Round(Total - AmountPaid, 2);
        if (BalanceDue <= 0 && Total > 0) Status = InvoiceStatus.Paid;
    }
}
