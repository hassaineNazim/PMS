namespace Pms.Application.Features.Staff;

public interface IStaffService
{
    Task<IReadOnlyList<StaffDto>> GetAllAsync(CancellationToken ct = default);
    Task<StaffDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StaffDto> CreateAsync(CreateStaffRequest request, CancellationToken ct = default);
    Task<StaffDto> UpdateAsync(Guid id, UpdateStaffRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ScheduleDto>> GetSchedulesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<ScheduleDto> CreateScheduleAsync(CreateScheduleRequest request, CancellationToken ct = default);
    Task DeleteScheduleAsync(Guid id, CancellationToken ct = default);
}
