using AccountantPortal.Data;
using AccountantPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountantPortal.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task NotifyAsync(string recipientUserId, string title, string body, CancellationToken cancellationToken = default)
    {
        _dbContext.NotificationMessages.Add(new NotificationMessage
        {
            RecipientUserId = recipientUserId,
            Title = title,
            Body = body
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationMessage>> UnreadForUserAsync(string recipientUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationMessages
            .Where(message => message.RecipientUserId == recipientUserId && !message.IsRead)
            .OrderByDescending(message => message.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
