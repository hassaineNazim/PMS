using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.CashRegister;

public record OpenCashSessionRequest(decimal OpeningFloat);
public record CloseCashSessionRequest(decimal CountedCash, string? Notes);

public record CashSessionDto(Guid Id, string UserName, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt,
    decimal OpeningFloat, decimal CashMovements, decimal ExpectedCash, decimal? CountedCash,
    decimal? Discrepancy, CashSessionStatus Status, string? Notes);

public interface ICashRegisterService
{
    Task<CashSessionDto?> GetCurrentAsync(CancellationToken ct = default);
    Task<CashSessionDto> OpenAsync(OpenCashSessionRequest request, CancellationToken ct = default);
    Task<CashSessionDto> CloseAsync(CloseCashSessionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CashSessionDto>> GetHistoryAsync(CancellationToken ct = default);
}

public class CashRegisterService(IApplicationDbContext db, ICurrentTenant tenant, ICurrentUser user)
    : ICashRegisterService
{
    public async Task<CashSessionDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        var s = await db.CashSessions.FirstOrDefaultAsync(
            x => x.Status == CashSessionStatus.Open && x.UserId == user.UserId, ct);
        if (s is null) return null;
        s.CashMovements = await CashMovementsAsync(s, ct);
        s.ExpectedCash = s.OpeningFloat + s.CashMovements;
        return Map(s);
    }

    public async Task<CashSessionDto> OpenAsync(OpenCashSessionRequest request, CancellationToken ct = default)
    {
        var existing = await db.CashSessions.AnyAsync(
            x => x.Status == CashSessionStatus.Open && x.UserId == user.UserId, ct);
        if (existing) throw new BusinessRuleException("A cash session is already open for this user.");

        var session = new CashSession
        {
            TenantId = tenant.TenantId,
            UserId = user.UserId ?? Guid.Empty,
            UserName = user.Email ?? "unknown",
            OpeningFloat = request.OpeningFloat,
            Status = CashSessionStatus.Open
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<CashSessionDto> CloseAsync(CloseCashSessionRequest request, CancellationToken ct = default)
    {
        var session = await db.CashSessions.FirstOrDefaultAsync(
            x => x.Status == CashSessionStatus.Open && x.UserId == user.UserId, ct)
            ?? throw new BusinessRuleException("No open cash session to close.");

        session.CashMovements = await CashMovementsAsync(session, ct);
        session.ExpectedCash = session.OpeningFloat + session.CashMovements;
        session.CountedCash = request.CountedCash;
        session.Discrepancy = decimal.Round(request.CountedCash - session.ExpectedCash, 2);
        session.ClosedAt = DateTimeOffset.UtcNow;
        session.Status = CashSessionStatus.Closed;
        session.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<IReadOnlyList<CashSessionDto>> GetHistoryAsync(CancellationToken ct = default) =>
        await db.CashSessions.OrderByDescending(s => s.OpenedAt).Take(50).Select(s => Map(s)).ToListAsync(ct);

    private async Task<decimal> CashMovementsAsync(CashSession s, CancellationToken ct) =>
        await db.Payments
            .Where(p => p.CashSessionId == s.Id && p.Method == PaymentMethod.Cash)
            .SumAsync(p => p.Type == PaymentType.Refund ? -p.Amount : p.Amount, ct);

    private static CashSessionDto Map(CashSession s) => new(
        s.Id, s.UserName, s.OpenedAt, s.ClosedAt, s.OpeningFloat, s.CashMovements, s.ExpectedCash,
        s.CountedCash, s.Discrepancy, s.Status, s.Notes);
}
