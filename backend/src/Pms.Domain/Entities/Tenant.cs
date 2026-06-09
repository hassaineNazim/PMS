using Pms.Domain.Common;

namespace Pms.Domain.Entities;

/// <summary>
/// A tenant is a single hotel/establishment that buys the product. All
/// operational data (rooms, guests, reservations…) is isolated per tenant.
/// Tenant rows themselves are NOT tenant-filtered (they sit above the filter).
/// </summary>
public class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;

    /// <summary>URL/host-friendly identifier used to resolve the tenant from a request.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? ContactEmail { get; set; }

    /// <summary>ISO currency code used for invoicing, e.g. "DZD", "EUR".</summary>
    public string Currency { get; set; } = "DZD";

    /// <summary>Default VAT/tax rate (percentage) applied to invoices.</summary>
    public decimal DefaultTaxRate { get; set; } = 9.00m;

    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // ---- Algerian legal / fiscal identifiers (mentions obligatoires DGI) ----
    /// <summary>NIF — Numéro d'Identification Fiscale.</summary>
    public string? TaxId { get; set; }
    /// <summary>NIS — Numéro d'Identification Statistique.</summary>
    public string? StatId { get; set; }
    /// <summary>RC — Registre du Commerce.</summary>
    public string? TradeRegister { get; set; }
    /// <summary>Article d'imposition.</summary>
    public string? TaxArticle { get; set; }

    // ---- Fiscal stamp (droit de timbre) on cash payments ----
    public bool FiscalStampEnabled { get; set; } = true;
    /// <summary>Rate of the droit de timbre on cash receipts (percent). Algeria: 1%.</summary>
    public decimal FiscalStampRate { get; set; } = 1.00m;
    /// <summary>Minimum stamp amount in DZD.</summary>
    public decimal FiscalStampMinimum { get; set; } = 5.00m;

    // ---- Meal plan supplements (per person, per night) ----
    public decimal BreakfastSupplement { get; set; }
    public decimal HalfBoardSupplement { get; set; }
    public decimal FullBoardSupplement { get; set; }

    // Navigation
    public License? License { get; set; }

    /// <summary>Per-person, per-night supplement for the given board type.</summary>
    public decimal MealSupplement(Enums.MealPlan plan) => plan switch
    {
        Enums.MealPlan.BedAndBreakfast => BreakfastSupplement,
        Enums.MealPlan.HalfBoard => HalfBoardSupplement,
        Enums.MealPlan.FullBoard => FullBoardSupplement,
        _ => 0m
    };

    /// <summary>
    /// Computes the droit de timbre due on a cash payment of <paramref name="cashAmount"/>.
    /// Algerian rule: 1 DA per 100 DA slice (or fraction thereof) — i.e.
    /// <see cref="FiscalStampRate"/> DA per started 100 DA — with a minimum of
    /// <see cref="FiscalStampMinimum"/> DA.
    /// </summary>
    public decimal ComputeFiscalStamp(decimal cashAmount)
    {
        if (!FiscalStampEnabled || cashAmount <= 0) return 0m;
        var slices = Math.Ceiling(cashAmount / 100m);
        var stamp = slices * FiscalStampRate;
        return Math.Max(stamp, FiscalStampMinimum);
    }
}
