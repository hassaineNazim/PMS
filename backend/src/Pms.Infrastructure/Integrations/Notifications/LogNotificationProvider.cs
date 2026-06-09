using Microsoft.Extensions.Logging;
using Pms.Application.Integrations;

namespace Pms.Infrastructure.Integrations.Notifications;

/// <summary>
/// Default notification provider: records the message to the log. Swap for an SMS
/// gateway (preferred in Algeria) or SMTP by implementing INotificationProvider —
/// the booking core stays untouched.
/// </summary>
public class LogNotificationProvider(ILogger<LogNotificationProvider> logger) : INotificationProvider
{
    public string Channel => "log";

    public Task<bool> SendAsync(string recipient, string subject, string message, CancellationToken ct = default)
    {
        logger.LogInformation("[notify:{Channel}] to={Recipient} subject={Subject} :: {Message}",
            Channel, recipient, subject, message);
        return Task.FromResult(true);
    }
}
