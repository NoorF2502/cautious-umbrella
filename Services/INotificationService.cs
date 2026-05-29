using AccountantPortal.Models;

namespace AccountantPortal.Services;

public interface INotificationService
{
    Task NotifyAsync(string recipientUserId, string title, string body, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationMessage>> UnreadForUserAsync(string recipientUserId, CancellationToken cancellationToken = default);
}
