using System.ComponentModel.DataAnnotations;
using CautiousUmbrella.Models;

namespace CautiousUmbrella.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class ClientRegisterViewModel
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    [Display(Name = "Preferred accountant")]
    public int? AccountantId { get; set; }

    public IEnumerable<Accountant> AvailableAccountants { get; set; } = Enumerable.Empty<Accountant>();
}

public class AccountantRegisterViewModel
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? OfficeAddress { get; set; }
}
