using System.ComponentModel.DataAnnotations;

namespace AccountantPortal.Models;

public class Report
{
    public int Id { get; set; }

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public int DocumentId { get; set; }
    public Document? Document { get; set; }

    public int AccountantId { get; set; }
    public Accountant? Accountant { get; set; }

    [DataType(DataType.Currency)]
    public decimal TaxableAmount { get; set; }

    public decimal TaxRate { get; set; }

    [DataType(DataType.Currency)]
    public decimal TaxAmount { get; set; }

    public DocumentStatus PaymentStatus { get; set; } = DocumentStatus.Unpaid;

    [StringLength(1000)]
    public string AccountantComments { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
