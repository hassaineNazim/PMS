using Pms.Application.Common;

namespace Pms.Application.Features.Guests;

public interface IGuestService
{
    Task<PagedResult<GuestDto>> SearchAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<GuestDto>> GetAllAsync(CancellationToken ct = default);
    Task<GuestDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GuestDto> CreateAsync(CreateGuestRequest request, CancellationToken ct = default);
    Task<GuestDto> UpdateAsync(Guid id, UpdateGuestRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
