using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Room : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public RoomType Type { get; set; } = RoomType.Single;
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public int? Floor { get; set; }
    public int Capacity { get; set; } = 1;
    public decimal PricePerNight { get; set; }
    public string? Description { get; set; }

    // ---- Housekeeping workflow (independent from commercial Status) ----
    public HousekeepingStatus HousekeepingStatus { get; set; } = HousekeepingStatus.Clean;
    /// <summary>Staff (housekeeper) currently assigned to service this room.</summary>
    public Guid? AssignedHousekeeperId { get; set; }
}
