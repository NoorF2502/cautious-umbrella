using System.ComponentModel.DataAnnotations;

namespace AccountantPortal.Models;

public class Document
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public int? AccountantId { get; set; }

    public Accountant? Accountant { get; set; }

    [Required, StringLength(160)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(80)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }

    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
