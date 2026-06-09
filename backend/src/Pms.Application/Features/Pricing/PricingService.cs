using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;

namespace Pms.Application.Features.Pricing;

public class PricingService(IApplicationDbContext db) : IPricingService
{
    public async Task<decimal> ComputeRoomTotalAsync(Room room, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        var periods = await LoadPeriodsAsync(checkIn, checkOut, ct);
        decimal total = 0m;
        for (var day = checkIn; day < checkOut; day = day.AddDays(1))
            total += Resolve(room, day, periods);
        return decimal.Round(total, 2);
    }

    public async Task<decimal> GetNightlyRateAsync(Room room, DateOnly date, CancellationToken ct = default)
    {
        var periods = await LoadPeriodsAsync(date, date.AddDays(1), ct);
        return Resolve(room, date, periods);
    }

    private async Task<List<RatePeriod>> LoadPeriodsAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        await db.RatePeriods
            .Where(p => p.StartDate < to && p.EndDate >= from)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(ct);

    private static decimal Resolve(Room room, DateOnly day, List<RatePeriod> periods)
    {
        var match = periods.FirstOrDefault(p =>
            (p.RoomType == null || p.RoomType == room.Type) && p.Covers(day));
        return match?.PricePerNight ?? room.PricePerNight;
    }
}
