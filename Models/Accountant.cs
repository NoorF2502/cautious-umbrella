using System.ComponentModel.DataAnnotations;

namespace AccountantPortal.Models;

public class Accountant
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(160)]
    public string OfficeAddress { get; set; } = string.Empty;

    [StringLength(80)]
    public string LicenseNumber { get; set; } = string.Empty;

    public ICollection<Client> Clients { get; set; } = new List<Client>();
}
