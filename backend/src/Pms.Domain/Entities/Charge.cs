using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>
/// An additional consumption posted to a guest folio (mini-bar, restaurant, room
/// service, laundry…). Accumulates on the reservation and flows onto the final
/// invoice. This is the minimal POS / "charges additionnelles sur la note".
/// </summary>
public class Charge : TenantEntity
{
    public Guid ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public ChargeCategory Category { get; set; } = ChargeCategory.Other;
    public string Label { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset PostedAt { get; set; } = DateTimeOffset.UtcNow;

    public void Recalculate() => Total = decimal.Round(Quantity * UnitPrice, 2);
}
