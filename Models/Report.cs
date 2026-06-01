using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CautiousUmbrella.Models;

public class Report
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    [Required]
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Income { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Expenses { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxableAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxDue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaymentAmount { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(2000)]
    public string? AccountantComments { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
}
