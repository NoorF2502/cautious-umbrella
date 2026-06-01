using System.ComponentModel.DataAnnotations;
using CautiousUmbrella.Models;

namespace CautiousUmbrella.ViewModels;

public class AccountantDashboardViewModel
{
    public Accountant Accountant { get; set; } = null!;
    public IReadOnlyList<Client> Clients { get; set; } = Array.Empty<Client>();
    public IReadOnlyList<Document> RecentDocuments { get; set; } = Array.Empty<Document>();
}

public class ClientDetailsViewModel
{
    public Client Client { get; set; } = null!;
    public IReadOnlyList<Document> Documents { get; set; } = Array.Empty<Document>();
    public IReadOnlyList<Report> Reports { get; set; } = Array.Empty<Report>();
}

public class CreateReportViewModel
{
    public int ClientId { get; set; }
    public int DocumentId { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Income { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Expenses { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxRatePercent { get; set; } = 15;

    [Range(0, double.MaxValue)]
    public decimal PaymentAmount { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(2000)]
    public string? AccountantComments { get; set; }

    public Client? Client { get; set; }
    public Document? Document { get; set; }
}
