using System.ComponentModel.DataAnnotations;

namespace CautiousUmbrella.Models;

public class Client
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Phone, StringLength(40)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    public int? AccountantId { get; set; }
    public Accountant? Accountant { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAtUtc { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
