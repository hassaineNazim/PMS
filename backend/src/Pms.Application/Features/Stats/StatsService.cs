using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Enums;

namespace Pms.Application.Features.Stats;

/// <summary>
/// Computes the dashboard KPIs and the 14-day time series for the charts.
/// Aggregations are done in-memory after a scoped fetch so the same code runs on
/// PostgreSQL in production and on the EF in-memory provider in unit tests.
/// </summary>
public class StatsService(IApplicationDbContext db) : IStatsService
{
    private const int Days = 14;

    public async Task<DashboardStatsDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-(Days - 1));

        var rooms = await db.Rooms.Select(r => r.Status).ToListAsync(ct);
        var reservations = await db.Reservations
            .Select(r => new { r.Status, r.CheckIn, r.CheckOut, r.CreatedAt }).ToListAsync(ct);
        var invoices = await db.Invoices
            .Select(i => new { i.Total, i.CreatedAt }).ToListAsync(ct);
        var guestCount = await db.Guests.CountAsync(ct);

        var roomStats = BuildRoomStats(rooms);
        var reservationStats = BuildReservationStats(reservations.Select(r => r.Status));
        var revenueStats = BuildRevenueStats(invoices.Select(i => (i.Total, i.CreatedAt)));

        var charts = new ChartsDto(
            BuildSeries(startDate, today, day =>
                reservations.Count(r => DateOnly.FromDateTime(r.CreatedAt.UtcDateTime) == day)),
            BuildSeries(startDate, today, day =>
                invoices.Where(i => DateOnly.FromDateTime(i.CreatedAt.UtcDateTime) == day).Sum(i => i.Total)),
            BuildSeries(startDate, today, day =>
            {
                if (rooms.Count == 0) return 0;
                var occupied = reservations.Count(r =>
                    (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.CheckedOut) &&
                    r.CheckIn <= day && r.CheckOut > day);
                return decimal.Round((decimal)occupied / rooms.Count * 100, 1);
            }),
            roomStats,
            reservationStats);

        return new DashboardStatsDto(roomStats, reservationStats, revenueStats, new GuestStatsDto(guestCount), charts);
    }

    private static RoomStatsDto BuildRoomStats(List<RoomStatus> rooms)
    {
        int total = rooms.Count;
        int occupied = rooms.Count(s => s == RoomStatus.Occupied);
        return new RoomStatsDto(
            total,
            rooms.Count(s => s == RoomStatus.Available),
            occupied,
            rooms.Count(s => s == RoomStatus.Dirty),
            rooms.Count(s => s == RoomStatus.OutOfService),
            total > 0 ? decimal.Round((decimal)occupied / total * 100, 1) : 0);
    }

    private static ReservationStatsDto BuildReservationStats(IEnumerable<ReservationStatus> statuses)
    {
        var list = statuses.ToList();
        return new ReservationStatsDto(
            list.Count,
            list.Count(s => s == ReservationStatus.Confirmed),
            list.Count(s => s == ReservationStatus.CheckedIn),
            list.Count(s => s == ReservationStatus.CheckedOut),
            list.Count(s => s == ReservationStatus.Cancelled));
    }

    private static RevenueStatsDto BuildRevenueStats(IEnumerable<(decimal Total, DateTimeOffset CreatedAt)> invoices)
    {
        var list = invoices.ToList();
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        decimal thisMonth = list.Where(i => i.CreatedAt.UtcDateTime >= thisMonthStart).Sum(i => i.Total);
        decimal lastMonth = list.Where(i => i.CreatedAt.UtcDateTime >= lastMonthStart && i.CreatedAt.UtcDateTime < thisMonthStart).Sum(i => i.Total);
        decimal total = list.Sum(i => i.Total);

        return new RevenueStatsDto(
            decimal.Round(total, 2),
            decimal.Round(thisMonth, 2),
            decimal.Round(lastMonth, 2),
            Growth(thisMonth, lastMonth),
            list.Count > 0 ? decimal.Round(total / list.Count, 2) : 0,
            list.Count);
    }

    private static List<TimeSeriesPoint> BuildSeries(DateOnly start, DateOnly end, Func<DateOnly, decimal> value)
    {
        var points = new List<TimeSeriesPoint>();
        for (var day = start; day <= end; day = day.AddDays(1))
            points.Add(new TimeSeriesPoint(day.ToString("MMM dd"), value(day)));
        return points;
    }

    private static decimal Growth(decimal current, decimal previous) =>
        previous == 0 ? 0 : decimal.Round((current - previous) / previous * 100, 1);
}
