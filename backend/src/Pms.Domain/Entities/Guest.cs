using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class Guest : TenantEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>2-letter language code used to localise the IPTV welcome screen.</summary>
    public string Language { get; set; } = "fr";

    public string? Nationality { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
