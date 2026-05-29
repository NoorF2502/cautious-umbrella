using Microsoft.AspNetCore.Identity;

namespace AccountantPortal.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Accountant? AccountantProfile { get; set; }
    public Client? ClientProfile { get; set; }
}
