using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Settings;

public record TenantSettingsDto(
    string Name, string LegalName, string? Address, string? City, string? Country,
    string? Phone, string? ContactEmail, string Currency, decimal DefaultTaxRate,
    string? TaxId, string? StatId, string? TradeRegister, string? TaxArticle,
    bool FiscalStampEnabled, decimal FiscalStampRate, decimal FiscalStampMinimum,
    decimal BreakfastSupplement, decimal HalfBoardSupplement, decimal FullBoardSupplement);

public interface ISettingsService
{
    Task<TenantSettingsDto> GetAsync(CancellationToken ct = default);
    Task<TenantSettingsDto> UpdateAsync(TenantSettingsDto dto, CancellationToken ct = default);
}

public class SettingsService(IApplicationDbContext db, ICurrentTenant tenant) : ISettingsService
{
    public async Task<TenantSettingsDto> GetAsync(CancellationToken ct = default) => Map(await LoadAsync(ct));

    public async Task<TenantSettingsDto> UpdateAsync(TenantSettingsDto d, CancellationToken ct = default)
    {
        var t = await LoadAsync(ct);
        t.Name = d.Name; t.LegalName = d.LegalName; t.Address = d.Address; t.City = d.City;
        t.Country = d.Country; t.Phone = d.Phone; t.ContactEmail = d.ContactEmail;
        t.Currency = d.Currency; t.DefaultTaxRate = d.DefaultTaxRate;
        t.TaxId = d.TaxId; t.StatId = d.StatId; t.TradeRegister = d.TradeRegister; t.TaxArticle = d.TaxArticle;
        t.FiscalStampEnabled = d.FiscalStampEnabled; t.FiscalStampRate = d.FiscalStampRate;
        t.FiscalStampMinimum = d.FiscalStampMinimum;
        t.BreakfastSupplement = d.BreakfastSupplement; t.HalfBoardSupplement = d.HalfBoardSupplement;
        t.FullBoardSupplement = d.FullBoardSupplement;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(t);
    }

    private async Task<Tenant> LoadAsync(CancellationToken ct) =>
        await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenant.TenantId, ct)
        ?? throw new NotFoundException(nameof(Tenant), tenant.TenantId);

    private static TenantSettingsDto Map(Tenant t) => new(
        t.Name, t.LegalName, t.Address, t.City, t.Country, t.Phone, t.ContactEmail, t.Currency, t.DefaultTaxRate,
        t.TaxId, t.StatId, t.TradeRegister, t.TaxArticle, t.FiscalStampEnabled, t.FiscalStampRate, t.FiscalStampMinimum,
        t.BreakfastSupplement, t.HalfBoardSupplement, t.FullBoardSupplement);
}
