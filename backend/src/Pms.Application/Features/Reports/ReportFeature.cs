using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Application.Integrations;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Reports;

public record MainCouranteEntryDto(DateOnly Date, string Movement, string GuestName, string RoomNumber,
    DateOnly CheckIn, DateOnly CheckOut, ReservationStatus Status);

public record PoliceFormPdf(string FileName, byte[] Content);

public interface IReportService
{
    /// <summary>Arrivals & departures journal (registre / main courante) for a date.</summary>
    Task<IReadOnlyList<MainCouranteEntryDto>> GetMainCouranteAsync(DateOnly date, CancellationToken ct = default);
    Task<byte[]> ExportReservationsCsvAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<byte[]> ExportRevenueCsvAsync(CancellationToken ct = default);
    Task<PoliceFormPdf> GeneratePoliceFormAsync(Guid reservationId, CancellationToken ct = default);
}

public class ReportService(IApplicationDbContext db, ICurrentTenant tenant, IPoliceFormGenerator police)
    : IReportService
{
    public async Task<IReadOnlyList<MainCouranteEntryDto>> GetMainCouranteAsync(DateOnly date, CancellationToken ct = default)
    {
        var rows = await db.Reservations.Include(r => r.Guest).Include(r => r.Room)
            .Where(r => r.CheckIn == date || r.CheckOut == date)
            .ToListAsync(ct);

        var entries = new List<MainCouranteEntryDto>();
        foreach (var r in rows)
        {
            if (r.CheckIn == date)
                entries.Add(new(date, "Arrivée", r.Guest?.FullName ?? "", r.Room?.Number ?? "", r.CheckIn, r.CheckOut, r.Status));
            if (r.CheckOut == date)
                entries.Add(new(date, "Départ", r.Guest?.FullName ?? "", r.Room?.Number ?? "", r.CheckIn, r.CheckOut, r.Status));
        }
        return entries.OrderBy(e => e.Movement).ThenBy(e => e.RoomNumber).ToList();
    }

    public async Task<byte[]> ExportReservationsCsvAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var q = db.Reservations.Include(r => r.Guest).Include(r => r.Room).AsQueryable();
        if (from.HasValue) q = q.Where(r => r.CheckOut > from.Value);
        if (to.HasValue) q = q.Where(r => r.CheckIn < to.Value);
        var rows = await q.OrderBy(r => r.CheckIn).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Client;Chambre;Arrivee;Depart;Nuits;Pension;Statut;MontantChambre;MontantPension");
        foreach (var r in rows)
            sb.AppendLine(string.Join(';',
                Csv(r.Guest?.FullName), Csv(r.Room?.Number), r.CheckIn, r.CheckOut, r.Nights,
                r.MealPlan, r.Status, Num(r.TotalAmount), Num(r.MealPlanTotal)));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportRevenueCsvAsync(CancellationToken ct = default)
    {
        var rows = await db.Invoices.Include(i => i.Guest)
            .OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Facture;Date;Client;SousTotal;TVA;Timbre;Total;Paye;Solde;Statut");
        foreach (var i in rows)
            sb.AppendLine(string.Join(';',
                i.Number, i.CreatedAt.ToString("yyyy-MM-dd"), Csv(i.Guest?.FullName),
                Num(i.Subtotal), Num(i.TaxAmount), Num(i.StampDuty), Num(i.Total), Num(i.AmountPaid), Num(i.BalanceDue), i.Status));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<PoliceFormPdf> GeneratePoliceFormAsync(Guid reservationId, CancellationToken ct = default)
    {
        var r = await db.Reservations.Include(x => x.Guest).Include(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == reservationId, ct)
            ?? throw new NotFoundException(nameof(Reservation), reservationId);
        var t = await db.Tenants.IgnoreQueryFilters().FirstAsync(x => x.Id == tenant.TenantId, ct);
        var bytes = police.Generate(r, r.Guest!, r.Room!, t);
        return new PoliceFormPdf($"fiche-police-{r.Room?.Number}-{r.CheckIn:yyyyMMdd}.pdf", bytes);
    }

    private static string Csv(string? v) => '"' + (v ?? "").Replace("\"", "\"\"") + '"';
    private static string Num(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
}
