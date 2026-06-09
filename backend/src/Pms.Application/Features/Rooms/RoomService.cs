using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Rooms;

public class RoomService(IApplicationDbContext db, ICurrentTenant tenant) : IRoomService
{
    public async Task<IReadOnlyList<RoomDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Rooms.OrderBy(r => r.Number).Select(r => Map(r)).ToListAsync(ct);

    public async Task<RoomDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Room), id);
        return Map(room);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        var number = request.Number.Trim();
        if (await db.Rooms.AnyAsync(r => r.Number == number, ct))
            throw new ConflictException($"A room numbered '{number}' already exists.");

        var room = new Room
        {
            TenantId = tenant.TenantId,
            Number = number,
            Type = request.Type,
            Floor = request.Floor,
            Capacity = request.Capacity,
            PricePerNight = request.PricePerNight,
            Description = request.Description,
            Status = Domain.Enums.RoomStatus.Available
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        return Map(room);
    }

    public async Task<RoomDto> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken ct = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Room), id);

        var number = request.Number.Trim();
        if (await db.Rooms.AnyAsync(r => r.Number == number && r.Id != id, ct))
            throw new ConflictException($"A room numbered '{number}' already exists.");

        room.Number = number;
        room.Type = request.Type;
        room.Status = request.Status;
        room.Floor = request.Floor;
        room.Capacity = request.Capacity;
        room.PricePerNight = request.PricePerNight;
        room.Description = request.Description;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(room);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Room), id);

        var hasReservations = await db.Reservations.AnyAsync(
            r => r.RoomId == id && Reservation.BlockingStatuses.Contains(r.Status), ct);
        if (hasReservations)
            throw new ConflictException("Cannot delete a room with active reservations.");

        db.Rooms.Remove(room);
        await db.SaveChangesAsync(ct);
    }

    private static RoomDto Map(Room r) =>
        new(r.Id, r.Number, r.Type, r.Status, r.Floor, r.Capacity, r.PricePerNight, r.Description);
}
