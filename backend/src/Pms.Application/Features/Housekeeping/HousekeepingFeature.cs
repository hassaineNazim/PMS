using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Housekeeping;

public record HousekeepingRoomDto(Guid RoomId, string Number, int? Floor, RoomStatus Status,
    HousekeepingStatus HousekeepingStatus, Guid? AssignedHousekeeperId, string? AssignedHousekeeperName);

public record AssignRequest(Guid? HousekeeperId);
public record SetHousekeepingStatusRequest(HousekeepingStatus Status);

public interface IHousekeepingService
{
    Task<IReadOnlyList<HousekeepingRoomDto>> GetBoardAsync(CancellationToken ct = default);
    Task<HousekeepingRoomDto> AssignAsync(Guid roomId, AssignRequest request, CancellationToken ct = default);
    Task<HousekeepingRoomDto> SetStatusAsync(Guid roomId, SetHousekeepingStatusRequest request, CancellationToken ct = default);
}

public class HousekeepingService(IApplicationDbContext db) : IHousekeepingService
{
    public async Task<IReadOnlyList<HousekeepingRoomDto>> GetBoardAsync(CancellationToken ct = default)
    {
        var rooms = await db.Rooms.OrderBy(r => r.Number).ToListAsync(ct);
        var staff = await db.Staff.ToDictionaryAsync(s => s.Id, s => s.FirstName + " " + s.LastName, ct);
        return rooms.Select(r => new HousekeepingRoomDto(
            r.Id, r.Number, r.Floor, r.Status, r.HousekeepingStatus, r.AssignedHousekeeperId,
            r.AssignedHousekeeperId is Guid id && staff.TryGetValue(id, out var n) ? n : null)).ToList();
    }

    public async Task<HousekeepingRoomDto> AssignAsync(Guid roomId, AssignRequest request, CancellationToken ct = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, ct)
            ?? throw new NotFoundException(nameof(Room), roomId);
        if (request.HousekeeperId is Guid hk && !await db.Staff.AnyAsync(s => s.Id == hk, ct))
            throw new NotFoundException("Staff", hk);
        room.AssignedHousekeeperId = request.HousekeeperId;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return await OneAsync(roomId, ct);
    }

    public async Task<HousekeepingRoomDto> SetStatusAsync(Guid roomId, SetHousekeepingStatusRequest request, CancellationToken ct = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, ct)
            ?? throw new NotFoundException(nameof(Room), roomId);
        room.HousekeepingStatus = request.Status;
        // When a room is cleaned & inspected and not occupied, free it commercially.
        if (request.Status == HousekeepingStatus.Inspected && room.Status == RoomStatus.Dirty)
            room.Status = RoomStatus.Available;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return await OneAsync(roomId, ct);
    }

    private async Task<HousekeepingRoomDto> OneAsync(Guid roomId, CancellationToken ct)
    {
        var board = await GetBoardAsync(ct);
        return board.First(r => r.RoomId == roomId);
    }
}
