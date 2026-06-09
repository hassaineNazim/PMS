namespace Pms.Application.Features.Stats;

public interface IStatsService
{
    Task<DashboardStatsDto> GetDashboardAsync(CancellationToken ct = default);
}
