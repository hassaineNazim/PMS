using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Rates;

public record RatePeriodDto(Guid Id, string Name, RoomType? RoomType, DateOnly StartDate,
    DateOnly EndDate, decimal PricePerNight, int Priority);

public record SaveRatePeriodRequest(string Name, RoomType? RoomType, DateOnly StartDate,
    DateOnly EndDate, decimal PricePerNight, int Priority);

public class SaveRatePeriodValidator : AbstractValidator<SaveRatePeriodRequest>
{
    public SaveRatePeriodValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.PricePerNight).GreaterThanOrEqualTo(0);
    }
}

public interface IRateService
{
    Task<IReadOnlyList<RatePeriodDto>> GetAllAsync(CancellationToken ct = default);
    Task<RatePeriodDto> CreateAsync(SaveRatePeriodRequest request, CancellationToken ct = default);
    Task<RatePeriodDto> UpdateAsync(Guid id, SaveRatePeriodRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class RateService(IApplicationDbContext db, ICurrentTenant tenant) : IRateService
{
    public async Task<IReadOnlyList<RatePeriodDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.RatePeriods.OrderByDescending(p => p.Priority).ThenBy(p => p.StartDate)
            .Select(p => Map(p)).ToListAsync(ct);

    public async Task<RatePeriodDto> CreateAsync(SaveRatePeriodRequest r, CancellationToken ct = default)
    {
        var p = new RatePeriod
        {
            TenantId = tenant.TenantId,
            Name = r.Name.Trim(), RoomType = r.RoomType, StartDate = r.StartDate,
            EndDate = r.EndDate, PricePerNight = r.PricePerNight, Priority = r.Priority
        };
        db.RatePeriods.Add(p);
        await db.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<RatePeriodDto> UpdateAsync(Guid id, SaveRatePeriodRequest r, CancellationToken ct = default)
    {
        var p = await db.RatePeriods.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(RatePeriod), id);
        p.Name = r.Name.Trim(); p.RoomType = r.RoomType; p.StartDate = r.StartDate;
        p.EndDate = r.EndDate; p.PricePerNight = r.PricePerNight; p.Priority = r.Priority;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await db.RatePeriods.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(RatePeriod), id);
        db.RatePeriods.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    private static RatePeriodDto Map(RatePeriod p) =>
        new(p.Id, p.Name, p.RoomType, p.StartDate, p.EndDate, p.PricePerNight, p.Priority);
}
