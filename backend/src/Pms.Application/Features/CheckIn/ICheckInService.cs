namespace Pms.Application.Features.CheckIn;

public interface ICheckInService
{
    Task<CheckInResult> CheckInAsync(Guid reservationId, CheckInRequest request, CancellationToken ct = default);
    Task<CheckOutResult> CheckOutAsync(Guid reservationId, CancellationToken ct = default);
}
