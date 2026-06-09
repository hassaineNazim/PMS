using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>
/// A seasonal rate override applying to a date range, optionally restricted to a
/// room type. The highest-priority matching period wins; otherwise the room's base
/// price applies (haute/basse saison).
/// </summary>
public class RatePeriod : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Null = applies to every room type.</summary>
    public RoomType? RoomType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public decimal PricePerNight { get; set; }

    /// <summary>Higher wins when several periods overlap.</summary>
    public int Priority { get; set; }

    public bool Covers(DateOnly date) => date >= StartDate && date <= EndDate;
}
