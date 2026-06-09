namespace Pms.Application.Integrations;

/// <summary>
/// Hardware-agnostic abstraction over an in-room display / signage system.
/// The default implementation targets LG (Pro:Centric / SuperSign), but isolating
/// behind this interface lets us support other screen brands for resold installs
/// WITHOUT touching the booking/check-in core. Register one implementation per
/// deployment (or a composite that fans out to several).
/// </summary>
public interface IDisplayProvider
{
    /// <summary>Provider identifier for logging/audit, e.g. "lg-procentric".</summary>
    string Name { get; }

    /// <summary>Pushes a personalised welcome to the room's screen at check-in.</summary>
    Task<DisplayResult> ShowWelcomeAsync(GuestDisplayInfo guest, CancellationToken ct = default);

    /// <summary>Clears the screen / resets to default at check-out.</summary>
    Task<DisplayResult> ClearAsync(string roomNumber, CancellationToken ct = default);
}

/// <summary>Data pushed to the in-room screen. Brand-neutral on purpose.</summary>
public record GuestDisplayInfo(
    string GuestName,
    string RoomNumber,
    DateOnly CheckOutDate,
    string Language,
    string HotelName);

public record DisplayResult(bool Success, string Provider, string? Detail = null, string? Error = null)
{
    public static DisplayResult Ok(string provider, string? detail = null) => new(true, provider, detail);
    public static DisplayResult Fail(string provider, string error) => new(false, provider, null, error);
}
