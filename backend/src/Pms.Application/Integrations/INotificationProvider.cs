namespace Pms.Application.Integrations;

/// <summary>
/// Channel-agnostic guest notification (booking confirmation, reminders…). In
/// Algeria SMS works better than email, so the channel is abstracted exactly like
/// the in-room display: swap the implementation without touching the core.
/// </summary>
public interface INotificationProvider
{
    string Channel { get; }
    Task<bool> SendAsync(string recipient, string subject, string message, CancellationToken ct = default);
}
