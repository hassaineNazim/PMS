using Microsoft.Extensions.Logging;
using Pms.Application.Integrations;

namespace Pms.Infrastructure.Integrations.Display;

/// <summary>
/// Default provider for sites without compatible in-room screens. It performs no
/// hardware I/O but records the intent, so check-in works everywhere out of the box.
/// </summary>
public class NullDisplayProvider(ILogger<NullDisplayProvider> logger) : IDisplayProvider
{
    public string Name => "none";

    public Task<DisplayResult> ShowWelcomeAsync(GuestDisplayInfo guest, CancellationToken ct = default)
    {
        logger.LogInformation("[display:none] Welcome {Guest} to room {Room}", guest.GuestName, guest.RoomNumber);
        return Task.FromResult(DisplayResult.Ok(Name, "no-op"));
    }

    public Task<DisplayResult> ClearAsync(string roomNumber, CancellationToken ct = default)
    {
        logger.LogInformation("[display:none] Clear room {Room}", roomNumber);
        return Task.FromResult(DisplayResult.Ok(Name, "no-op"));
    }
}
