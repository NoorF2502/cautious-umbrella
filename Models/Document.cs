using System.ComponentModel.DataAnnotations;

namespace CautiousUmbrella.Models;

public enum DocumentStatus
{
    Pending = 0,
    Reviewed = 1
}

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1
}

public class Document
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    [Required, StringLength(180)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }

    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
