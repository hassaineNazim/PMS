using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class StaffSchedule : TenantEntity
{
    public Guid StaffId { get; set; }
    public Staff? Staff { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly ShiftStart { get; set; }
    public TimeOnly ShiftEnd { get; set; }
    public string? Notes { get; set; }
}
