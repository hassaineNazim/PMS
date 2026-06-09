using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Application.Features.Pricing;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Reservations;

public class ReservationService(IApplicationDbContext db, ICurrentTenant tenant, IPricingService pricing) : IReservationService
{
    private async Task<Tenant> CurrentTenantAsync(CancellationToken ct) =>
        await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenant.TenantId, ct)
        ?? throw new NotFoundException(nameof(Tenant), tenant.TenantId);

    public async Task<IReadOnlyList<ReservationDto>> GetAllAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var query = db.Reservations.Include(r => r.Guest).Include(r => r.Room).AsQueryable();
        if (from.HasValue) query = query.Where(r => r.CheckOut > from.Value);
        if (to.HasValue) query = query.Where(r => r.CheckIn < to.Value);

        return await query.OrderBy(r => r.CheckIn).Select(r => Map(r)).ToListAsync(ct);
    }

    public async Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var res = await db.Reservations.Include(r => r.Guest).Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Reservation), id);
        return Map(res);
    }

    public async Task<IReadOnlyList<AvailableRoomDto>> GetAvailableRoomsAsync(AvailabilityRequest request, CancellationToken ct = default)
    {
        if (request.CheckOut <= request.CheckIn)
            throw new BusinessRuleException("Check-out must be after check-in.");

        var nights = Math.Max(1, request.CheckOut.DayNumber - request.CheckIn.DayNumber);

        var query = db.Rooms.Where(r => r.Status != RoomStatus.OutOfService);
        if (request.Type.HasValue) query = query.Where(r => r.Type == request.Type.Value);

        // A room is free when it has no blocking reservation overlapping [CheckIn, CheckOut).
        var available = query.Where(r => !db.Reservations.Any(res =>
            res.RoomId == r.Id &&
            Reservation.BlockingStatuses.Contains(res.Status) &&
            res.CheckIn < request.CheckOut &&
            res.CheckOut > request.CheckIn));

        return await available
            .OrderBy(r => r.Number)
            .Select(r => new AvailableRoomDto(
                r.Id, r.Number, r.Type, r.Capacity, r.PricePerNight,
                nights, decimal.Round(r.PricePerNight * nights, 2)))
            .ToListAsync(ct);
    }

    public async Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        if (request.CheckOut <= request.CheckIn)
            throw new BusinessRuleException("Check-out must be after check-in.");

        var guest = await db.Guests.FirstOrDefaultAsync(g => g.Id == request.GuestId, ct)
            ?? throw new NotFoundException(nameof(Guest), request.GuestId);
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new NotFoundException(nameof(Room), request.RoomId);

        await EnsureNoOverlapAsync(request.RoomId, request.CheckIn, request.CheckOut, null, ct);

        var tenantEntity = await CurrentTenantAsync(ct);
        var roomTotal = await pricing.ComputeRoomTotalAsync(room, request.CheckIn, request.CheckOut, ct);
        var reservation = new Reservation
        {
            TenantId = tenant.TenantId,
            GuestId = request.GuestId,
            RoomId = request.RoomId,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Status = ReservationStatus.Confirmed,
            Adults = request.Adults,
            Children = request.Children,
            MealPlan = request.MealPlan,
            MealPlanSupplement = tenantEntity.MealSupplement(request.MealPlan),
            Notes = request.Notes,
            Source = request.Source,
            TotalAmount = roomTotal
        };
        db.Reservations.Add(reservation);
        // The PostgreSQL EXCLUDE constraint guarantees no double-booking even if two
        // receptionists pass the check above concurrently; SaveChanges maps the
        // resulting 23P01 violation to a ConflictException.
        await db.SaveChangesAsync(ct);

        reservation.Guest = guest;
        reservation.Room = room;
        return Map(reservation);
    }

    public async Task<ReservationDto> UpdateAsync(Guid id, UpdateReservationRequest request, CancellationToken ct = default)
    {
        var res = await db.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Reservation), id);
        if (res.Status is ReservationStatus.CheckedOut or ReservationStatus.Cancelled)
            throw new BusinessRuleException($"Cannot edit a {res.Status} reservation.");
        if (request.CheckOut <= request.CheckIn)
            throw new BusinessRuleException("Check-out must be after check-in.");

        _ = await db.Guests.FirstOrDefaultAsync(g => g.Id == request.GuestId, ct)
            ?? throw new NotFoundException(nameof(Guest), request.GuestId);
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new NotFoundException(nameof(Room), request.RoomId);

        await EnsureNoOverlapAsync(request.RoomId, request.CheckIn, request.CheckOut, id, ct);

        var tenantEntity = await CurrentTenantAsync(ct);
        res.GuestId = request.GuestId;
        res.RoomId = request.RoomId;
        res.CheckIn = request.CheckIn;
        res.CheckOut = request.CheckOut;
        res.Adults = request.Adults;
        res.Children = request.Children;
        res.MealPlan = request.MealPlan;
        res.MealPlanSupplement = tenantEntity.MealSupplement(request.MealPlan);
        res.Notes = request.Notes;
        res.TotalAmount = await pricing.ComputeRoomTotalAsync(room, request.CheckIn, request.CheckOut, ct);
        res.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ReservationDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var res = await db.Reservations.Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Reservation), id);
        if (res.Status == ReservationStatus.CheckedOut)
            throw new BusinessRuleException("Cannot cancel a checked-out reservation.");

        res.Status = ReservationStatus.Cancelled;
        res.UpdatedAt = DateTimeOffset.UtcNow;
        // Free the room if it was being held/occupied by this booking.
        if (res.Room is { Status: RoomStatus.Occupied })
            res.Room.Status = RoomStatus.Dirty;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    private async Task EnsureNoOverlapAsync(Guid roomId, DateOnly checkIn, DateOnly checkOut, Guid? excludeId, CancellationToken ct)
    {
        var overlaps = await db.Reservations.AnyAsync(r =>
            r.RoomId == roomId &&
            (excludeId == null || r.Id != excludeId) &&
            Reservation.BlockingStatuses.Contains(r.Status) &&
            r.CheckIn < checkOut &&
            r.CheckOut > checkIn, ct);
        if (overlaps)
            throw new ConflictException("The room is already booked for the selected dates.");
    }

    private static ReservationDto Map(Reservation r) => new(
        r.Id, r.GuestId, r.Guest?.FullName ?? string.Empty,
        r.RoomId, r.Room?.Number ?? string.Empty, r.Room?.Type ?? RoomType.Single,
        r.CheckIn, r.CheckOut, r.Nights, r.Status, r.Adults, r.Children,
        r.MealPlan, r.MealPlanTotal, r.TotalAmount, r.TotalAmount + r.MealPlanTotal,
        r.Notes, r.AccompanyingGuests);
}
