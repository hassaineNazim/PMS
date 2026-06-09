namespace Pms.Application.Features.Reservations;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetAllAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AvailableRoomDto>> GetAvailableRoomsAsync(AvailabilityRequest request, CancellationToken ct = default);
    Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task<ReservationDto> UpdateAsync(Guid id, UpdateReservationRequest request, CancellationToken ct = default);
    Task<ReservationDto> CancelAsync(Guid id, CancellationToken ct = default);
}
