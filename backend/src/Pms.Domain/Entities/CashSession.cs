using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>
/// A receptionist's cash drawer session. Opened with a float, closed at end of
/// shift with a counted amount; the system computes the expected cash and the
/// discrepancy (clôture de caisse).
/// </summary>
public class CashSession : TenantEntity
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }

    public decimal OpeningFloat { get; set; }

    /// <summary>Sum of cash movements during the session (computed at close).</summary>
    public decimal CashMovements { get; set; }

    /// <summary>Expected cash = opening float + cash movements.</summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>Physically counted amount at close.</summary>
    public decimal? CountedCash { get; set; }

    /// <summary>CountedCash − ExpectedCash (over/short).</summary>
    public decimal? Discrepancy { get; set; }

    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;
    public string? Notes { get; set; }
}
