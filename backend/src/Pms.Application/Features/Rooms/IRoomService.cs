namespace Pms.Application.Features.Rooms;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoomDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<RoomDto> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
