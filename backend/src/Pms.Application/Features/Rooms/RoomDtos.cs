using Pms.Domain.Enums;

namespace Pms.Application.Features.Rooms;

public record RoomDto(
    Guid Id,
    string Number,
    RoomType Type,
    RoomStatus Status,
    int? Floor,
    int Capacity,
    decimal PricePerNight,
    string? Description);

public record CreateRoomRequest(
    string Number,
    RoomType Type,
    int? Floor,
    int Capacity,
    decimal PricePerNight,
    string? Description);

public record UpdateRoomRequest(
    string Number,
    RoomType Type,
    RoomStatus Status,
    int? Floor,
    int Capacity,
    decimal PricePerNight,
    string? Description);
