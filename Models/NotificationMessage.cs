using System.ComponentModel.DataAnnotations;

namespace AccountantPortal.Models;

public class NotificationMessage
{
    public int Id { get; set; }

    [Required]
    public string RecipientUserId { get; set; } = string.Empty;

    public ApplicationUser? RecipientUser { get; set; }

    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(600)]
    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
