using Pms.Domain.Entities;

namespace Pms.Application.Features.Pricing;

/// <summary>Resolves nightly rates, applying seasonal rate periods over the room base price.</summary>
public interface IPricingService
{
    /// <summary>Pre-tax room charge for the stay, summing each night's applicable rate.</summary>
    Task<decimal> ComputeRoomTotalAsync(Room room, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);

    /// <summary>Effective nightly rate for a room on a given date (seasonal override or base).</summary>
    Task<decimal> GetNightlyRateAsync(Room room, DateOnly date, CancellationToken ct = default);
}
