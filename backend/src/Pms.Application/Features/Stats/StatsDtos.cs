namespace Pms.Application.Features.Stats;

public record RoomStatsDto(int Total, int Available, int Occupied, int Dirty, int OutOfService, decimal OccupancyRate);

public record ReservationStatsDto(int Total, int Confirmed, int CheckedIn, int CheckedOut, int Cancelled);

public record RevenueStatsDto(decimal Total, decimal ThisMonth, decimal LastMonth, decimal MonthGrowth, decimal AverageInvoice, int InvoiceCount);

public record GuestStatsDto(int Total);

public record TimeSeriesPoint(string Date, decimal Value);

public record ChartsDto(
    IReadOnlyList<TimeSeriesPoint> ReservationsByDay,
    IReadOnlyList<TimeSeriesPoint> RevenueByDay,
    IReadOnlyList<TimeSeriesPoint> OccupancyByDay,
    RoomStatsDto RoomsByStatus,
    ReservationStatsDto ReservationsByStatus);

public record DashboardStatsDto(
    RoomStatsDto Rooms,
    ReservationStatsDto Reservations,
    RevenueStatsDto Revenue,
    GuestStatsDto Guests,
    ChartsDto Charts);
