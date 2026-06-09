using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>
/// A room booking over a [CheckIn, CheckOut) date range. Overlap protection is
/// enforced at three levels: application validation, an EF transaction, and a
/// PostgreSQL EXCLUDE constraint (the ultimate guarantee against double-booking
/// even under concurrent receptionists).
/// </summary>
public class Reservation : TenantEntity
{
    public Guid GuestId { get; set; }
    public Guest? Guest { get; set; }

    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>Inclusive arrival date.</summary>
    public DateOnly CheckIn { get; set; }

    /// <summary>Exclusive departure date (guest leaves the morning of CheckOut).</summary>
    public DateOnly CheckOut { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

    public int Adults { get; set; } = 1;
    public int Children { get; set; }
    public string? AccompanyingGuests { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }

    /// <summary>Board / meal plan (formule de pension) — the TV-driven signature feature.</summary>
    public MealPlan MealPlan { get; set; } = MealPlan.RoomOnly;

    /// <summary>Per-person, per-night meal supplement snapshot at booking time.</summary>
    public decimal MealPlanSupplement { get; set; }

    /// <summary>Total room charge captured at booking time (nights × price), pre-tax.</summary>
    public decimal TotalAmount { get; set; }

    public int Nights => Math.Max(1, CheckOut.DayNumber - CheckIn.DayNumber);

    public int Occupants => Math.Max(1, Adults + Children);

    /// <summary>Meal plan charge = nights × occupants × supplement.</summary>
    public decimal MealPlanTotal => decimal.Round(Nights * Occupants * MealPlanSupplement, 2);

    /// <summary>Statuses that actually occupy the room for overlap purposes.</summary>
    public static readonly ReservationStatus[] BlockingStatuses =
        [ReservationStatus.Confirmed, ReservationStatus.CheckedIn];
}
