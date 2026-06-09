using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pms.Application.Common;
using Pms.Application.Integrations;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.CheckIn;

/// <summary>
/// Orchestrates the check-in flow atomically:
///   reservation -> CheckedIn, room -> Occupied, invoice created, audit logged.
/// The in-room display (IPTV) push is best-effort: a failure is logged to the
/// audit trail but never rolls back the check-in.
/// </summary>
public class CheckInService(
    IApplicationDbContext db,
    ICurrentTenant tenant,
    IDisplayProvider display,
    ILogger<CheckInService> logger) : ICheckInService
{
    public async Task<CheckInResult> CheckInAsync(Guid reservationId, CheckInRequest request, CancellationToken ct = default)
    {
        var reservation = await db.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new NotFoundException(nameof(Reservation), reservationId);

        if (reservation.Status == ReservationStatus.CheckedIn)
            throw new BusinessRuleException("Guest is already checked in.");
        if (reservation.Status != ReservationStatus.Confirmed)
            throw new BusinessRuleException($"Cannot check in a reservation with status {reservation.Status}.");

        var room = reservation.Room ?? throw new NotFoundException(nameof(Room), reservation.RoomId);
        if (room.Status == RoomStatus.Occupied)
            throw new ConflictException("The room is already occupied.");

        var tenantEntity = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == reservation.TenantId, ct)
            ?? throw new NotFoundException(nameof(Tenant), reservation.TenantId);

        Invoice invoice = null!;
        await db.ExecuteInTransactionAsync(async token =>
        {
            reservation.Status = ReservationStatus.CheckedIn;
            reservation.AccompanyingGuests = request.AccompanyingGuests;
            reservation.UpdatedAt = DateTimeOffset.UtcNow;
            room.Status = RoomStatus.Occupied;
            room.UpdatedAt = DateTimeOffset.UtcNow;

            invoice = await BuildInvoiceAsync(reservation, room, tenantEntity, token);
            db.Invoices.Add(invoice);

            AddAudit("check_in", "reservation", reservation.Id, new
            {
                guest = reservation.Guest?.FullName,
                room = room.Number
            }, true);
            AddAudit("invoice_created", "invoice", invoice.Id, new
            {
                invoice.Number,
                invoice.Total
            }, true);

            await db.SaveChangesAsync(token);
        }, ct);

        // Best-effort IPTV / signage push (outside the transaction).
        var displayResult = await TryNotifyDisplayAsync(reservation, room, tenantEntity, ct);

        return new CheckInResult(
            reservation.Id,
            reservation.Guest?.FullName ?? string.Empty,
            room.Number,
            reservation.CheckOut,
            invoice.Id,
            invoice.Number,
            invoice.Total,
            displayResult.Success,
            displayResult.Provider,
            displayResult.Error);
    }

    public async Task<CheckOutResult> CheckOutAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await db.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new NotFoundException(nameof(Reservation), reservationId);

        if (reservation.Status != ReservationStatus.CheckedIn)
            throw new BusinessRuleException($"Cannot check out a reservation with status {reservation.Status}.");

        var room = reservation.Room!;
        await db.ExecuteInTransactionAsync(async token =>
        {
            reservation.Status = ReservationStatus.CheckedOut;
            reservation.UpdatedAt = DateTimeOffset.UtcNow;
            room.Status = RoomStatus.Dirty; // needs housekeeping before re-letting
            room.UpdatedAt = DateTimeOffset.UtcNow;

            AddAudit("check_out", "reservation", reservation.Id, new
            {
                guest = reservation.Guest?.FullName,
                room = room.Number
            }, true);

            await db.SaveChangesAsync(token);
        }, ct);

        var cleared = false;
        try
        {
            var result = await display.ClearAsync(room.Number, ct);
            cleared = result.Success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Display clear failed for room {Room}", room.Number);
        }

        return new CheckOutResult(reservation.Id, reservation.Guest?.FullName ?? string.Empty, room.Number, cleared);
    }

    private async Task<Invoice> BuildInvoiceAsync(Reservation reservation, Room room, Tenant tenant, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var countThisYear = await db.Invoices.CountAsync(i => i.CreatedAt.Year == year, ct);
        var number = $"INV-{year}-{(countThisYear + 1):D5}";

        var invoice = new Invoice
        {
            TenantId = reservation.TenantId,
            Number = number,
            ReservationId = reservation.Id,
            GuestId = reservation.GuestId,
            RoomId = reservation.RoomId,
            CheckIn = reservation.CheckIn,
            CheckOut = reservation.CheckOut,
            Nights = reservation.Nights,
            PricePerNight = room.PricePerNight,
            RoomSubtotal = reservation.TotalAmount,        // seasonal-aware room charge
            MealPlanSubtotal = reservation.MealPlanTotal,  // board / pension
            TaxRate = tenant.DefaultTaxRate,
            Currency = tenant.Currency,
            Status = InvoiceStatus.Pending
        };
        invoice.Recalculate();
        return invoice;
    }

    private async Task<DisplayResult> TryNotifyDisplayAsync(Reservation reservation, Room room, Tenant tenant, CancellationToken ct)
    {
        try
        {
            var info = new GuestDisplayInfo(
                reservation.Guest?.FullName ?? string.Empty,
                room.Number,
                reservation.CheckOut,
                reservation.Guest?.Language ?? "en",
                tenant.Name);
            var result = await display.ShowWelcomeAsync(info, ct);
            AddAudit("display_notification", "reservation", reservation.Id, result, result.Success,
                result.Success ? null : result.Error);
            await db.SaveChangesAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Display welcome push failed for reservation {Id}", reservation.Id);
            AddAudit("display_notification", "reservation", reservation.Id,
                new { error = ex.Message }, false, ex.Message);
            await db.SaveChangesAsync(ct);
            return DisplayResult.Fail(display.Name, ex.Message);
        }
    }

    private void AddAudit(string action, string entityType, Guid entityId, object details, bool success, string? error = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.TenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = JsonSerializer.Serialize(details),
            Success = success,
            ErrorMessage = error
        });
    }
}
