using System.ComponentModel.DataAnnotations;

namespace AccountantPortal.Models;

public class Client
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    public int? AccountantId { get; set; }

    public Accountant? Accountant { get; set; }

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(120)]
    public string BusinessName { get; set; } = string.Empty;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
