using System.ComponentModel.DataAnnotations;
using CautiousUmbrella.Models;
using Microsoft.AspNetCore.Http;

namespace CautiousUmbrella.ViewModels;

public class ClientDashboardViewModel
{
    public Client Client { get; set; } = null!;
    public Accountant? Accountant { get; set; }
    public IReadOnlyList<Document> Documents { get; set; } = Array.Empty<Document>();
    public IReadOnlyList<Report> Reports { get; set; } = Array.Empty<Report>();
}

public class DocumentUploadViewModel
{
    [Required]
    [Display(Name = "Document file")]
    public IFormFile File { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }
}
