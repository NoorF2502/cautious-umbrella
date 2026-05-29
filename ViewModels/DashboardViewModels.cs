using AccountantPortal.Models;

namespace AccountantPortal.ViewModels;

public class ClientDashboardViewModel
{
    public Client Client { get; set; } = new();
    public Accountant? Accountant { get; set; }
    public IReadOnlyList<Document> RecentDocuments { get; set; } = Array.Empty<Document>();
    public IReadOnlyList<Report> Reports { get; set; } = Array.Empty<Report>();
    public IReadOnlyList<NotificationMessage> Notifications { get; set; } = Array.Empty<NotificationMessage>();
}

public class AccountantDashboardViewModel
{
    public Accountant Accountant { get; set; } = new();
    public IReadOnlyList<Client> Clients { get; set; } = Array.Empty<Client>();
    public IReadOnlyList<Document> PendingDocuments { get; set; } = Array.Empty<Document>();
    public IReadOnlyList<Document> PaidDocuments { get; set; } = Array.Empty<Document>();
    public IReadOnlyList<NotificationMessage> Notifications { get; set; } = Array.Empty<NotificationMessage>();
}

public class DocumentUploadViewModel
{
    public IFormFile? File { get; set; }
}

public class ReportCreateViewModel
{
    public int DocumentId { get; set; }
    public string AccountantComments { get; set; } = string.Empty;
}
