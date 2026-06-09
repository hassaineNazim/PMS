using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Exceptions;
using StaffEntity = Pms.Domain.Entities.Staff;
using ScheduleEntity = Pms.Domain.Entities.StaffSchedule;

namespace Pms.Application.Features.Staff;

public class StaffService(IApplicationDbContext db, ICurrentTenant tenant) : IStaffService
{
    public async Task<IReadOnlyList<StaffDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Staff.OrderBy(s => s.LastName).Select(s => Map(s)).ToListAsync(ct);

    public async Task<StaffDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await db.Staff.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Staff", id);
        return Map(s);
    }

    public async Task<StaffDto> CreateAsync(CreateStaffRequest request, CancellationToken ct = default)
    {
        var s = new StaffEntity
        {
            TenantId = tenant.TenantId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim().ToLower(),
            Phone = request.Phone?.Trim(),
            Role = request.Role,
            Department = request.Department,
            HireDate = request.HireDate,
            Status = request.Status
        };
        db.Staff.Add(s);
        await db.SaveChangesAsync(ct);
        return Map(s);
    }

    public async Task<StaffDto> UpdateAsync(Guid id, UpdateStaffRequest request, CancellationToken ct = default)
    {
        var s = await db.Staff.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Staff", id);
        s.FirstName = request.FirstName.Trim();
        s.LastName = request.LastName.Trim();
        s.Email = request.Email?.Trim().ToLower();
        s.Phone = request.Phone?.Trim();
        s.Role = request.Role;
        s.Department = request.Department;
        s.HireDate = request.HireDate;
        s.Status = request.Status;
        s.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(s);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var s = await db.Staff.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Staff", id);
        db.Staff.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ScheduleDto>> GetSchedulesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var query = db.StaffSchedules.Include(s => s.Staff).AsQueryable();
        if (from.HasValue) query = query.Where(s => s.Date >= from.Value);
        if (to.HasValue) query = query.Where(s => s.Date <= to.Value);

        return await query.OrderBy(s => s.Date).ThenBy(s => s.ShiftStart)
            .Select(s => new ScheduleDto(s.Id, s.StaffId,
                s.Staff!.FirstName + " " + s.Staff.LastName,
                s.Date, s.ShiftStart, s.ShiftEnd, s.Notes))
            .ToListAsync(ct);
    }

    public async Task<ScheduleDto> CreateScheduleAsync(CreateScheduleRequest request, CancellationToken ct = default)
    {
        var staff = await db.Staff.FirstOrDefaultAsync(x => x.Id == request.StaffId, ct)
            ?? throw new NotFoundException("Staff", request.StaffId);

        var schedule = new ScheduleEntity
        {
            TenantId = tenant.TenantId,
            StaffId = request.StaffId,
            Date = request.Date,
            ShiftStart = request.ShiftStart,
            ShiftEnd = request.ShiftEnd,
            Notes = request.Notes
        };
        db.StaffSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);

        return new ScheduleDto(schedule.Id, staff.Id, staff.FullName,
            schedule.Date, schedule.ShiftStart, schedule.ShiftEnd, schedule.Notes);
    }

    public async Task DeleteScheduleAsync(Guid id, CancellationToken ct = default)
    {
        var s = await db.StaffSchedules.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("StaffSchedule", id);
        db.StaffSchedules.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    private static StaffDto Map(StaffEntity s) =>
        new(s.Id, s.FirstName, s.LastName, s.FullName, s.Email, s.Phone, s.Role, s.Department, s.HireDate, s.Status);
}
