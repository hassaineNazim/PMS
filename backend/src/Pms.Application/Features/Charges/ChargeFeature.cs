using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Charges;

public record ChargeDto(Guid Id, Guid ReservationId, ChargeCategory Category, string Label,
    int Quantity, decimal UnitPrice, decimal Total, DateTimeOffset PostedAt);

public record CreateChargeRequest(Guid ReservationId, ChargeCategory Category, string Label,
    int Quantity, decimal UnitPrice);

public class CreateChargeValidator : AbstractValidator<CreateChargeRequest>
{
    public CreateChargeValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

public interface IChargeService
{
    Task<IReadOnlyList<ChargeDto>> GetForReservationAsync(Guid reservationId, CancellationToken ct = default);
    Task<ChargeDto> CreateAsync(CreateChargeRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class ChargeService(IApplicationDbContext db, ICurrentTenant tenant, ICurrentUser user) : IChargeService
{
    public async Task<IReadOnlyList<ChargeDto>> GetForReservationAsync(Guid reservationId, CancellationToken ct = default) =>
        await db.Charges.Where(c => c.ReservationId == reservationId)
            .OrderBy(c => c.PostedAt).Select(c => Map(c)).ToListAsync(ct);

    public async Task<ChargeDto> CreateAsync(CreateChargeRequest request, CancellationToken ct = default)
    {
        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct)
            ?? throw new NotFoundException(nameof(Reservation), request.ReservationId);
        if (reservation.Status is ReservationStatus.CheckedOut or ReservationStatus.Cancelled)
            throw new BusinessRuleException("Cannot post charges to a closed reservation.");

        var charge = new Charge
        {
            TenantId = tenant.TenantId,
            ReservationId = request.ReservationId,
            Category = request.Category,
            Label = request.Label.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            CreatedByUserId = user.UserId
        };
        charge.Recalculate();
        db.Charges.Add(charge);
        await db.SaveChangesAsync(ct);
        return Map(charge);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var charge = await db.Charges.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Charge), id);
        db.Charges.Remove(charge);
        await db.SaveChangesAsync(ct);
    }

    private static ChargeDto Map(Charge c) =>
        new(c.Id, c.ReservationId, c.Category, c.Label, c.Quantity, c.UnitPrice, c.Total, c.PostedAt);
}
